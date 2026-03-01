using System.Net.Http.Headers;
using System.Net.Http.Json;
using PaymentService.Application.Features.Users.DTOs;

namespace PaymentService.Api.Tests.Helpers;

public static class AuthHelper
{
    public static async Task<AuthResponse> RegisterUserAsync(
        HttpClient client,
        string phoneNumber,
        string email,
        string fullName = "Test Testov",
        string password = "Passw0rd1")
    {
        var registerRequest = new RegisterRequest(phoneNumber, email, fullName, password);

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!;
    }

    public static async Task<AuthResponse> LoginUserAsync(
        HttpClient client,
        string phoneNumber,
        string password = "Passw0rd1")
    {
        var loginRequest = new LoginRequest(phoneNumber, password);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!;
    }

    public static async Task<AuthResponse> RegisterAndLoginUserAsync(
        HttpClient client,
        string phoneNumber,
        string email,
        string fullName = "Test Testov",
        string password = "Passw0rd1")
    {
        await RegisterUserAsync(client, phoneNumber, email, fullName, password);
        return await LoginUserAsync(client, phoneNumber, password);
    }

    public static void SetBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static HttpRequestMessage CreateRequestWithIdempotencyKey(
        HttpMethod method, 
        string url, 
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }
}