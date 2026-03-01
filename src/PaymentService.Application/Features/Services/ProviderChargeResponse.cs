namespace PaymentService.Application.Features.Services;

public sealed record ProviderChargeResponse(
    ProviderChargeStatus Status,
    string? ProviderReferenceId,
    string? ErrorMessage);

public enum ProviderChargeStatus
{
    Succeeded,
    Failed,
    Unavailable,
    Timeout
}
