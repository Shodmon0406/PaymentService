using PaymentService.Domain.Enums.Orders;

namespace PaymentService.Application.Features.Orders.DTOs;

public sealed record OrderResponse(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Currency,
    OrderStatus Status,
    DateTimeOffset CreatedAt);