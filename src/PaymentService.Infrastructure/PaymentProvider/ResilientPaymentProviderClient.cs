using PaymentService.Application.Features.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace PaymentService.Infrastructure.PaymentProvider;

public sealed class ResilientPaymentProviderClient(IPaymentProviderClient innerClient) : IPaymentProviderClient
{
    private readonly ResiliencePipeline<ProviderChargeResponse> _resiliencePipeline = BuildResiliencePipeline();

    public async Task<ProviderChargeResponse> ChargeAsync(ProviderChargeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => await innerClient.ChargeAsync(request, ct),
                cancellationToken);
        }
        catch (TimeoutRejectedException)
        {
            return new ProviderChargeResponse(ProviderChargeStatus.Timeout, null,
                "Payment provider did not respond in time.");
        }
        catch (BrokenCircuitException)
        {
            return new ProviderChargeResponse(ProviderChargeStatus.Unavailable, null,
                "Payment provider circuit is open; too many recent failures.");
        }
        catch (ProviderUnavailableException ex)
        {
            return new ProviderChargeResponse(ProviderChargeStatus.Unavailable, null, ex.Message);
        }
    }

    private static ResiliencePipeline<ProviderChargeResponse> BuildResiliencePipeline()
    {
        var shouldHandle = new PredicateBuilder<ProviderChargeResponse>()
            .Handle<ProviderUnavailableException>()
            .Handle<TimeoutRejectedException>();

        return new ResiliencePipelineBuilder<ProviderChargeResponse>()
            .AddTimeout(TimeSpan.FromSeconds(2))
            .AddRetry(new RetryStrategyOptions<ProviderChargeResponse>
            {
                ShouldHandle = shouldHandle,
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromMicroseconds(100)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<ProviderChargeResponse>
            {
                ShouldHandle = shouldHandle,
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build();
    }
}