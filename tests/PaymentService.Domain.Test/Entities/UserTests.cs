using FluentAssertions;
using PaymentService.Domain.Entities.Users;

namespace PaymentService.Domain.Test.Entities;

public class UserTests
{
    private const string ValidPhoneNumber = "+992123456789";
    private const string ValidEmail = "me@shodmon.ru";
    private const string ValidFullName = "Shodmon Inoyatzoda";
    private const string ValidPassword = "P@ssw0rd!";

    [Fact]
    public void Register_ValidData_ReturnsSuccess()
    {
        // Act
        var result = User.Register(ValidPhoneNumber, ValidEmail, ValidFullName, ValidPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = result.Value;
        user.PhoneNumber.Value.Should().Be(ValidPhoneNumber);
        user.Email?.Value.Should().Be(ValidEmail);
        user.FullName.Should().Be(ValidFullName);
        user.PasswordHash.Should().NotBe(ValidPassword); // Should be hashed
    }

    [Theory]
    [InlineData("invalid-phone", ValidEmail, ValidFullName, ValidPassword)]
    [InlineData(ValidPhoneNumber, "invalid-email", ValidFullName, ValidPassword)]
    [InlineData(ValidPhoneNumber, ValidEmail, ValidFullName, "short")]
    public void Register_InvalidData_ReturnsFailure(string phone, string email, string fullName, string password)
    {
        // Act
        var result = User.Register(phone, email, fullName, password);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(ValidPhoneNumber, ValidEmail, ValidFullName, "P@ssw0rd!")]
    [InlineData(ValidPhoneNumber, ValidEmail, ValidFullName, "AnotherP@ssw0rd!")]
    public void Register_SamePassword_HashShouldBeDifferent(string phone, string email, string fullName, string password)
    {
        // Act
        var result1 = User.Register(phone, email, fullName, password);
        var result2 = User.Register(phone, email, fullName, password);
        
        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        var user1 = result1.Value;
        var user2 = result2.Value;
        user1.PasswordHash.Should().NotBe(user2.PasswordHash); // Hashes should be different due to salting
    }

    [Theory]
    [InlineData(ValidPhoneNumber, ValidEmail, ValidFullName, "P@ssw0rd!")]
    [InlineData(ValidPhoneNumber, ValidEmail, ValidFullName, "AnotherP@ssw0rd!")]
    public void Register_SamePassword_VerifyPasswordShouldSucceed(string phone, string email, string fullName, string password)
    {
        // Act
        var result = User.Register(phone, email, fullName, password);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = result.Value;
        user.VerifyPassword(password).Should().BeTrue();
    }

    [Theory]
    [InlineData(ValidPhoneNumber, ValidEmail, ValidFullName, "P@ssw0rd!", "P@ssw0rd!", "NewP@ssw0rd!")]
    [InlineData(ValidPhoneNumber, ValidEmail, ValidFullName, "P@ssw0rd!", "WrongCurrentPassword", "NewP@ssw0rd!")]
    public void ChangePassword_VariousScenarios(string phone, string email, string fullName, string initialPassword,
        string currentPassword, string newPassword)
    {
        // Arrange
        var registerResult = User.Register(phone, email, fullName, initialPassword);
        registerResult.IsSuccess.Should().BeTrue();
        var user = registerResult.Value;
        
        // Act
        var changePasswordResult = user.ChangePassword(currentPassword, newPassword);
        
        // Assert
        if (currentPassword == initialPassword)
        {
            changePasswordResult.IsSuccess.Should().BeTrue();
            user.VerifyPassword(newPassword).Should().BeTrue();
        }
        else
        {
            changePasswordResult.IsFailure.Should().BeTrue();
            user.VerifyPassword(initialPassword).Should().BeTrue(); // Password should not change
        }
    }

    [Fact]
    public void RecordLogin_ShouldUpdateLastLoginInfo()
    {
        // Arrange
        var registerResult = User.Register(ValidPhoneNumber, ValidEmail, ValidFullName, ValidPassword);
        registerResult.IsSuccess.Should().BeTrue();
        var user = registerResult.Value;
        const string ipAddress = "192.168.10.01";
        var beforeLogin = DateTimeOffset.UtcNow;

        // Act
        user.RecordLogin(ipAddress);
        
        // Assert
        user.LastLoginAt.Should().BeAfter(beforeLogin);
    }
}