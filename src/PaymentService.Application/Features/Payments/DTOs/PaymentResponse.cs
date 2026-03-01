using PaymentService.Domain.Enums.Payments;

namespace PaymentService.Application.Features.Payments.DTOs;

public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    DateTimeOffset CreatedAt);