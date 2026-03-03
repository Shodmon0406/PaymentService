using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.Tests.Common;
using PaymentService.Api.Tests.Helpers;
using PaymentService.Application.Features.Users.DTOs;

namespace PaymentService.Api.Tests.Auth;

[Collection(nameof(PaymentServiceCollection))]
public class AuthEndpointsTests(PaymentServiceWebApplicationFactory factory)
{
    [Fact]
    public async Task Register_Should_Return_Ok()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var auth = await AuthHelper.RegisterUserAsync(client, "+992123456789", "test1@mail100.com");

        // Assert
        auth.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task Login_Should_Return_Ok()
    {
        // Arrange
        var client = factory.CreateClient();
        await AuthHelper.RegisterUserAsync(client, "+992123456780", "test1231@mail.com");

        // Act
        var auth = await AuthHelper.LoginUserAsync(client, "+992123456780");

        // Assert
        auth.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task Me_Should_Return_Current_User()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992121456781", "test3@mail100.com");
        AuthHelper.SetBearerToken(client, auth.AccessToken);

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = (await response.Content.ReadFromJsonAsync<CurrentUserResponse>())!;
        user.FullName.Should().Be("Test User");
        user.PhoneNumber.Should().Be("+992121456781");
        user.Email.Should().Be("test3@mail100.com");
    }

    [Fact]
    public async Task Me_Without_Token_Should_Return_Unauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Should_Return_New_Access_Token()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123156782", "test4@mail100.com");

        // Act
        var refreshRequest = new RefreshTokenRequest(auth.RefreshToken);
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        refreshResponse.Should().NotBeNull();
        refreshResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Verify the new access token works
        var newClient = factory.CreateClient();
        AuthHelper.SetBearerToken(newClient, refreshResponse.AccessToken);
        var meResponse = await newClient.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_With_Invalid_Token_Should_Return_Unauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        const string invalidRefreshToken = "invalid-token";

        // Act
        var refreshRequest = new RefreshTokenRequest(invalidRefreshToken);
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Revoke_Should_Invalidate_Refresh_Token()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123416783", "test5@mail100.com");

        var revokeRequest = new RevokeRefreshTokenRequest(auth.RefreshToken);
        AuthHelper.SetBearerToken(client, auth.AccessToken);

        // Act
        var revokeResponse = await client.PostAsJsonAsync("/api/v1/auth/revoke", revokeRequest);

        // Assert
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to refresh with the revoked token
        var refreshRequest = new RefreshTokenRequest(auth.RefreshToken);
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Duplicate_PhoneNumber_Should_Return_Conflict()
    {
        // Arrange
        var client = factory.CreateClient();
        await AuthHelper.RegisterUserAsync(client, "+992123456784", "test6@mail100.com");
        
        var request = new RegisterRequest("+992123456784", "test7@mail100.com", "Test User", "Passw0rd1");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}