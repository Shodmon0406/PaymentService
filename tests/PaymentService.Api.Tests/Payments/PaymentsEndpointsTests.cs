using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.Tests.Common;
using PaymentService.Api.Tests.Helpers;
using PaymentService.Application.Features.Orders.DTOs;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Domain.Enums.Orders;
using PaymentService.Domain.Enums.Payments;

namespace PaymentService.Api.Tests.Payments;

[Collection(nameof(PaymentServiceCollection))]
public class PaymentsEndpointsTests(PaymentServiceWebApplicationFactory factory)
{
    private async Task<(HttpClient client, OrderResponse)> SetupOrderAsync(string phone, string email102)
    {
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, phone, email102);
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));

        var createOrderResponse = await client.PostAsJsonAsync("/api/v1/orders",
            new CreateOrderRequest(200.00m, "USD"));

        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await createOrderResponse.Content.ReadFromJsonAsync<OrderResponse>())!;

        return (client, order);
    }

    [Fact]
    public async Task CreatePayment_Return200_WithPendingStatus()
    {
        // Arrange
        var (client, order) = await SetupOrderAsync("+992123456791", "payment1@mail102.com");

        // Act
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        var response = await client.PostAsync($"/api/v1/payments/{order.Id}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payment = (await response.Content.ReadFromJsonAsync<PaymentResponse>())!;
        payment.OrderId.Should().Be(order.Id);
        payment.Amount.Should().Be(order.Amount);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task CreatePayment_Idempotent_ReturnsSamePayment()
    {
        // Arrange
        var (client, order) = await SetupOrderAsync("+992123456792", "payment2@mail102.com");
        var key = Guid.NewGuid().ToString();

        // Act
        var request1 = AuthHelper.CreateRequestWithIdempotencyKey(HttpMethod.Post, $"api/v1/payments/{order.Id}", key);
        var request2 = AuthHelper.CreateRequestWithIdempotencyKey(HttpMethod.Post, $"api/v1/payments/{order.Id}", key);

        var response1 = await client.SendAsync(request1);
        var response2 = await client.SendAsync(request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var payment1 = (await response1.Content.ReadFromJsonAsync<PaymentResponse>())!;
        var payment2 = (await response2.Content.ReadFromJsonAsync<PaymentResponse>())!;

        payment1.Id.Should().Be(payment2.Id);
        payment1.OrderId.Should().Be(order.Id);
        payment1.Amount.Should().Be(order.Amount);
        payment1.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task ListPayments_ReturnsPaymentsForOrder()
    {
        // Arrange
        var (client, order) = await SetupOrderAsync("+992123456793", "test123@mail102.com");

        // Act
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        await client.PostAsync($"/api/v1/payments/{order.Id}", null);
        var listResponse = await client.GetAsync($"/api/v1/payments/{order.Id}");

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = (await listResponse.Content.ReadFromJsonAsync<List<PaymentResponse>>())!;
        payments.Should().ContainSingle(p => p.OrderId == order.Id);
    }

    [Fact]
    public async Task CreatePayment_Unauthenticated_Returns401()
    {
        // Arrange
        var client = factory.CreateClient();
        var orderId = Guid.NewGuid();

        // Act
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        var response = await client.PostAsync($"/api/v1/payments/{orderId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPayments_ForNonExistentOrder_Returns404()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123456794", "test4@mail102.com");
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        var orderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/payments/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePayment_ForNonExistentOrder_Returns404()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123456795", "test5@mail102.com");
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        var orderId = Guid.NewGuid();

        // Act
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        var response = await client.PostAsync($"/api/v1/payments/{orderId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConfirmPayment_Returns200_WithCompletedStatus()
    {
        // Arrange
        var (client, order) = await SetupOrderAsync("+992123456796", "test6@mail102.com");
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        var createPaymentResponse = await client.PostAsync($"/api/v1/payments/{order.Id}", null);
        var payment = (await createPaymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;

        // Act
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        var confirmResponse = await client.PostAsync($"/api/v1/payments/{payment.Id}/confirm", null);

        // Assert
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedPayment = (await confirmResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        confirmedPayment.Id.Should().Be(payment.Id);
        confirmedPayment.OrderId.Should().Be(order.Id);
        confirmedPayment.Amount.Should().Be(order.Amount);
        confirmedPayment.Status.Should().Be(PaymentStatus.Successful);
        
        // Order should now be Paid
        var orderResponse = await client.GetAsync($"/api/v1/orders/{order.Id}");
        var updatedOrder = (await orderResponse.Content.ReadFromJsonAsync<OrderResponse>())!;
        updatedOrder.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task ConfirmPayment_ForNonExistentPayment_Returns404()
    {
        // Arrange
        var client = factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLoginUserAsync(client, "+992123456797", "test7@mail102.com");
        AuthHelper.SetBearerToken(client, auth.AccessToken);
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        var paymentId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/v1/payments/{paymentId}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConfirmPayment_SameIdempotent_ReturnsSameResult()
    {
        // Arrange
        var (client, order) = await SetupOrderAsync("+992123456798", "test8@mail102.com");
        AuthHelper.SetIdempotencyKey(client, Guid.NewGuid().ToString("N"));
        var createPaymentResponse = await client.PostAsync($"/api/v1/payments/{order.Id}", null);
        var payment = (await createPaymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!;
        var key = Guid.NewGuid().ToString("N");

        // Act
        AuthHelper.SetIdempotencyKey(client, key);
        var response1 = await client.PostAsync($"/api/v1/payments/{payment.Id}/confirm", null);
        AuthHelper.SetIdempotencyKey(client, key);
        var response2 = await client.PostAsync($"/api/v1/payments/{payment.Id}/confirm", null);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var confirmedPayment1 = (await response1.Content.ReadFromJsonAsync<PaymentResponse>())!;
        var confirmedPayment2 = (await response2.Content.ReadFromJsonAsync<PaymentResponse>())!;
        
        confirmedPayment1.Id.Should().Be(confirmedPayment2.Id);
        confirmedPayment1.OrderId.Should().Be(confirmedPayment2.OrderId);
        confirmedPayment1.Amount.Should().Be(confirmedPayment2.Amount);
        confirmedPayment1.Status.Should().Be(PaymentStatus.Successful);
        confirmedPayment1.Status.Should().Be(confirmedPayment2.Status);
    }
}