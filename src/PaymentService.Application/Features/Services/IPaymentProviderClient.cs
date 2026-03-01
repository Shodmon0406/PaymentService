namespace PaymentService.Application.Features.Services;

public interface IPaymentProviderClient
{
    Task<ProviderChargeResponse> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken);
}