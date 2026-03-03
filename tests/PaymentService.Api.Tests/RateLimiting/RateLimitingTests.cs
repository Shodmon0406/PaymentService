using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.IntegrationTests.Infrastructure;
using PaymentService.Api.Tests.Common;
using PaymentService.Api.Tests.Helpers;
using PaymentService.Application.Features.Users.DTOs;

namespace PaymentService.Api.Tests.RateLimiting;

[Collection(nameof(RateLimitingCollection))]
public sealed class RateLimitingTests(RateLimitingWebApplicationFactory factory)
{
    // Auth endpoint rate limiting
    [Fact]
    public async Task AuthEndpoint_Returns429_AfterPermitLimitExceeded()
    {
        var client = factory.CreateClient();
        const string uniqueIp = "127.0.0.1";

        for (var i = 0; i < 2; i++)
        {
            var req = BuildLoginRequest(uniqueIp);
            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
                because: $"request {i + 1} should pass through the rate limiter");
        }

        var limitedReq = BuildLoginRequest(uniqueIp);
        var limitedResp = await client.SendAsync(limitedReq);

        limitedResp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AuthEndpoint_429Response_IncludesRetryAfterHeader()
    {
        var client = factory.CreateClient();
        const string uniqueIp = "127.0.0.2";

        for (var i = 0; i < 2; i++)
            await client.SendAsync(BuildLoginRequest(uniqueIp));

        var limitedResp = await client.SendAsync(BuildLoginRequest(uniqueIp));

        limitedResp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limitedResp.Headers.Should().ContainKey("Retry-After");
    }

    // Per-user rate limiting (authenticated)
    [Fact]
    public async Task AuthenticatedUser_IsLimited_ByUserId()
    {
        var setupClient = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(
            setupClient, "+992123456781", "rl_user1@test.com");

        var client = factory.CreateClient();
        AuthHelper.SetBearerToken(client, auth.AccessToken);

        const string ipA = "127.0.0.1";
        const string ipB = "127.0.0.2";

        var req1 = BuildLoginRequest(ipA);
        req1.Headers.Authorization = client.DefaultRequestHeaders.Authorization;
        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

        var req2 = BuildLoginRequest(ipB);
        req2.Headers.Authorization = client.DefaultRequestHeaders.Authorization;
        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

        var req3 = BuildLoginRequest(ipA);
        req3.Headers.Authorization = client.DefaultRequestHeaders.Authorization;
        var resp3 = await client.SendAsync(req3);
        resp3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // Per-IP rate limiting (unauthenticated)
    [Fact]
    public async Task UnauthenticatedRequests_AreLimited_ByIp()
    {
        var client = factory.CreateClient();
        const string ipA = "10.99.3.1";
        const string ipB = "10.99.3.2";

        for (var i = 0; i < 2; i++)
            await client.SendAsync(BuildLoginRequest(ipA));

        var limitedForA = await client.SendAsync(BuildLoginRequest(ipA));
        limitedForA.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var notLimitedForB = await client.SendAsync(BuildLoginRequest(ipB));
        notLimitedForB.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    private static HttpRequestMessage BuildLoginRequest(string forwardedIp)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest("+10000000000", "wrong_password"))
        };
        msg.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedIp);
        return msg;
    }
}
