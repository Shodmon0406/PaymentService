using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PaymentService.Application.Auth;
using PaymentService.Application.Features.Users.Commands.Login;
using PaymentService.Application.Features.Users.Commands.RefreshToken;
using PaymentService.Application.Features.Users.Commands.Register;
using PaymentService.Application.Features.Users.Commands.RevokeRefreshToken;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Application.Features.Users.Queries.GetCurrentUser;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Users;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Application.Tests.Users;

public class UserHandlerTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public UserHandlerTest()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();

        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _jwtTokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");

        _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
        _refreshTokenGenerator.Generate().Returns(_ => Guid.NewGuid().ToString("N"));
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task<AuthResponse> RegisterUserAsync(
        string phone = "992901234567",
        string email = "test@mail.com",
        string fullName = "Test User",
        string password = "Password1")
    {
        var handler = new RegisterCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);
        var command = new RegisterCommand(phone, email, fullName, password, "127.0.0.1");
        var result = await handler.Handle(command, CancellationToken.None);
        return result.Value;
    }

    [Fact]
    public async Task Register_ShouldSucceed()
    {
        // Arrange
        var handler = new RegisterCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);
        var command = new RegisterCommand("+998901234567", "test@mail.com", "Test User", "Password1", "127.0.0.1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var authResponse = result.Value;
        authResponse.AccessToken.Should().Be("access-token");
        authResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResponse.UserId.Should().NotBeEmpty();
        authResponse.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task Register_DuplicatePhone_ShouldFail()
    {
        // Arrange
        await RegisterUserAsync(phone: "+992901234567", email: "test1@mail.com");
        var handler = new RegisterCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);
        var command = new RegisterCommand("+992901234567", "test2@mail.com", "Other User", "Password1", "127.0.0.1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldFail()
    {
        // Arrange
        await RegisterUserAsync(phone: "+992901234567", email: "test@mail.com");
        var handler = new RegisterCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);
        var command = new RegisterCommand("+992907654321", "test@mail.com", "Other User", "Password1", "127.0.0.1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Register_InvalidPassword_ShouldFail()
    {
        // Arrange
        var handler = new RegisterCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);
        var command = new RegisterCommand("+998901234567", "test@mail.com", "Test User", "pass", "127.0.0.1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        await RegisterUserAsync();
        var handler = new LoginCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);

        // Act
        var result = await handler.Handle(new LoginCommand("992901234567", "Password1", "127.0.0.1"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var authResponse = result.Value;
        authResponse.AccessToken.Should().Be("access-token");
        authResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResponse.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        await RegisterUserAsync();
        var handler = new LoginCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);

        // Act
        var result = await handler.Handle(new LoginCommand("+998901234567", "WrongPassword", "127.0.0.1"),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonexistentPhone_ShouldFail()
    {
        // Arrange
        var handler = new LoginCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);

        // Act
        var result = await handler.Handle(new LoginCommand("+998901234567", "Password1", "127.0.0.1"),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldSucceed()
    {
        // Arrange
        var registered = await RegisterUserAsync();
        var originalToken = registered.RefreshToken;

        var handler = new RefreshTokenCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);

        // Act
        var result = await handler.Handle(new RefreshTokenCommand(originalToken, "127.0.0.1"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var authResponse = result.Value;
        authResponse.AccessToken.Should().Be("access-token");
        authResponse.RefreshToken.Should().NotBe(originalToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldFail()
    {
        // Arrange
        var handler = new RefreshTokenCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);

        // Act
        var result =
            await handler.Handle(new RefreshTokenCommand("invalid-token", "127.0.0.1"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_ShouldFail()
    {
        // Arrange
        var registered = await RegisterUserAsync();
        var originalToken = registered.RefreshToken;

        var ctx = CreateContext();
        var refreshToken = await ctx.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == originalToken);
        refreshToken!.Revoke("127.0.0.1", "Testing revoke");
        await ctx.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(CreateContext(), _jwtTokenService, _refreshTokenGenerator);

        // Act
        var result = await handler.Handle(new RefreshTokenCommand(originalToken, "127.0.0.1"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task RevokeRefreshToken_WithValidToken_ShouldSucceed()
    {
        // Arrange
        var registered = await RegisterUserAsync();
        var originalToken = registered.RefreshToken;

        var handler = new RevokeRefreshTokenCommandHandler(CreateContext());

        // Act
        var result = await handler.Handle(new RevokeRefreshTokenCommand(originalToken, "127.0.0.1"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ctx = CreateContext();
        var refreshToken = await ctx.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == originalToken);
        refreshToken.Should().NotBeNull();
        refreshToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshToken_WithInvalidToken_ShouldFail()
    {
        // Arrange
        var handler = new RevokeRefreshTokenCommandHandler(CreateContext());

        // Act
        var result = await handler.Handle(new RevokeRefreshTokenCommand("invalid-token", "127.0.0.1"),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RevokeRefreshToken_WithAlreadyRevokedToken_ShouldFail()
    {
        // Arrange
        var registered = await RegisterUserAsync();
        var originalToken = registered.RefreshToken;

        var handler = new RevokeRefreshTokenCommandHandler(CreateContext());
        await handler.Handle(new RevokeRefreshTokenCommand(originalToken, "127.0.0.1"), CancellationToken.None);

        // Act
        var result = await handler.Handle(new RevokeRefreshTokenCommand(originalToken, "127.0.0.1"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUser()
    {
        // Arrange
        var registered = await RegisterUserAsync();
        var userId = registered.UserId;

        var ctx = CreateContext();
        var handler = new GetCurrentUserQueryHandler(ctx);

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(userId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var userDto = result.Value;
        userDto.Id.Should().Be(userId);
        userDto.FullName.Should().Be(registered.FullName);
        userDto.PhoneNumber.Should().Be("992901234567");
        userDto.Email.Should().Be("test@mail.com");
    }

    [Fact]
    public async Task GetCurrentUser_WithNonexistentUser_ShouldFail()
    {
        // Arrange
        var handler = new GetCurrentUserQueryHandler(CreateContext());

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}