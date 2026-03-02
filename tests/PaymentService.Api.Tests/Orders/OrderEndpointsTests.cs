using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.Tests.Common;
using PaymentService.Api.Tests.Helpers;
using PaymentService.Application.Features.Orders.DTOs;

namespace PaymentService.Api.Tests.Orders;

[Collection(nameof(PaymentServiceCollection))]
public class OrderEndpointsTests(PaymentServiceWebApplicationFactory factory)
{
    [Fact]
    public async Task CreateOrder_Return201_And_GetOrder_ReturnsSameOrder()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123456781", "order1@mail101.com");
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));

        var request = new CreateOrderRequest(100m, "TJS");

        // Act
        var createResponse = await client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.Amount.Should().Be(100m);
        created.Currency.Should().Be("TJS");
        created.UserId.Should().Be(auth.UserId);
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var getResponse = await client.GetAsync($"/api/v1/orders/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        fetched.Should().NotBeNull();
        fetched.Id.Should().Be(created.Id);
        fetched.Amount.Should().Be(created.Amount);
        fetched.Currency.Should().Be(created.Currency);
        fetched.UserId.Should().Be(created.UserId);
        fetched.CreatedAt.Should().BeCloseTo(created.CreatedAt, TimeSpan.FromMicroseconds(100));
    }

    [Fact]
    public async Task GetOrder_Unauthenticated_Return401()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrder_ForAnotherUsersOrder_Return404()
    {
        // Arrange
        var client1 = factory.CreateClient();
        var auth1 = await AuthHelper.RegisterAndLoginUserAsync(client1, "+992123456782", "order2@mail101.com");
        AuthHelper.SetBearerToken(client1, auth1.AccessToken);
        AuthHelper.SetIdempotencyKey(client1, Guid.NewGuid().ToString("N"));
        
        var createResponse = await client1.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(100m, "TJS"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order1 = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        order1.Should().NotBeNull();
        
        var client2 = factory.CreateClient();
        var auth2 = await AuthHelper.RegisterAndLoginUserAsync(client2, "+992123456783", "order3@mail101.com");
        AuthHelper.SetBearerToken(client2, auth2.AccessToken);
        AuthHelper.SetIdempotencyKey(client2, Guid.NewGuid().ToString("N"));
        
        // Act
        var getResponse = await client2.GetAsync($"/api/v1/orders/{order1.Id}");
        
        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithNegativeAmount_Return400()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123451784", "test4@mail101.com");
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        
        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest(-100m, "TJS"));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithSameIdempotencyKey_ReturnSameOrder()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123156781", "order01@mail101.com");
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));

        var request = new CreateOrderRequest(100m, "TJS");

        // Act
        var createResponse1 = await client.PostAsJsonAsync("/api/v1/orders", request);
        var createResponse2 = await client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        createResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        var created1 = await createResponse1.Content.ReadFromJsonAsync<OrderResponse>();
        created1.Should().NotBeNull();
        created1.Id.Should().NotBeEmpty();
        createResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        var created2 = await createResponse2.Content.ReadFromJsonAsync<OrderResponse>();
        created2.Should().NotBeNull();
        created2.Id.Should().NotBeEmpty();
        created1.Id.Should().Be(created2.Id);
    }
}