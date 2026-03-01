using Microsoft.Extensions.Options;
using PaymentService.Application.Features.Services;

namespace PaymentService.Infrastructure.PaymentProvider;

public sealed class FakePaymentProviderClient : IPaymentProviderClient
{
    private readonly FakePaymentProviderOptions _options;
    
    public FakePaymentProviderClient(IOptions<FakePaymentProviderOptions> options)
    {
        _options = options.Value;
    }
    
    public FakePaymentProviderClient(FakePaymentProviderOptions options)
    {
        _options = options;
    }

    public async Task<ProviderChargeResponse> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken)
    {
        if (_options.DelayMs > 0)
            await Task.Delay(_options.DelayMs, cancellationToken);
        
        if (_options.AlwaysUnavailable)
            throw new ProviderUnavailableException($"Fake provider is configured as unavailable for payment '{request.PaymentId}'.");
        
        if (_options.AlwaysDecline)
            return new ProviderChargeResponse(ProviderChargeStatus.Failed, null, $"Fake provider declined payment '{request.PaymentId}'.");
        
        if (_options.SuccessRate < 1.0)
        {
            var hash = (uint)request.PaymentId.GetHashCode();
            var fraction = hash % 100 / 100.0;
            if (fraction >= _options.SuccessRate)
                throw new ProviderUnavailableException($"Fake provider simulated transient failure for payment '{request.PaymentId}'.");
        }

        return new ProviderChargeResponse(ProviderChargeStatus.Succeeded, $"FAKE-{request.PaymentId:N}", null);
    }
}