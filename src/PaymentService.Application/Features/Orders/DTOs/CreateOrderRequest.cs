namespace PaymentService.Application.Features.Orders.DTOs;

public sealed record CreateOrderRequest(decimal Amount, string Currency);