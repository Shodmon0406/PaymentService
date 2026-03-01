namespace PaymentService.Application.Features.Services;

public sealed record ProviderChargeRequest(
    Guid PaymentId,
    decimal Amount,
    string Currency);