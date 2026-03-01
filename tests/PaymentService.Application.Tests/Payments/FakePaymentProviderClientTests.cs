using System.Diagnostics;
using FluentAssertions;
using PaymentService.Application.Features.Services;
using PaymentService.Infrastructure.PaymentProvider;

namespace PaymentService.Application.Tests.Payments;

public class FakePaymentProviderClientTests
{
    private static ProviderChargeRequest MakeRequest() => new(Guid.NewGuid(), Amount: 100, Currency: "TJS");

    [Fact]
    public async Task FakeProvider_WhenSuccessRateIs1_AlwaysSucceeds()
    {
        // Arrange
        var provider = new FakePaymentProviderClient(new FakePaymentProviderOptions { SuccessRate = 1.0 });
        var request = MakeRequest();

        // Act
        var result = await provider.ChargeAsync(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ProviderChargeStatus.Succeeded);
        result.ProviderReferenceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FakeProvider_WhenAlwaysUnavailable_ThrowsUnavailableException()
    {
        // Arrange
        var provider = new FakePaymentProviderClient(new FakePaymentProviderOptions { AlwaysUnavailable = true });
        var request = MakeRequest();

        // Act
        var act = async () => await provider.ChargeAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task FakeProvider_WhenAlwaysDecline_ReturnsFailed()
    {
        // Arrange
        var provider = new FakePaymentProviderClient(new FakePaymentProviderOptions { AlwaysDecline = true });
        var request = MakeRequest();

        // Act
        var result = await provider.ChargeAsync(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ProviderChargeStatus.Failed);
        result.ProviderReferenceId.Should().BeNull();
    }

    [Fact]
    public async Task FakeProvider_IsDeterministicForSamePaymentId()
    {
        // Arrange
        var options = new FakePaymentProviderOptions { SuccessRate = 0.5 };
        var provider = new FakePaymentProviderClient(options);
        var request = MakeRequest();

        // Act & Assert
        ProviderChargeStatus? firstOutcome = null;
        for (var i = 0; i < 5; i++)
        {
            ProviderChargeStatus outcome;
            try
            {
                var result = await provider.ChargeAsync(request, CancellationToken.None);
                outcome = result.Status;
            }
            catch (ProviderUnavailableException)
            {
                outcome = ProviderChargeStatus.Unavailable;
            }

            if (firstOutcome == null)
                firstOutcome = outcome;
            else
                outcome.Should().Be(firstOutcome.Value);
        }
    }

    [Fact]
    public async Task FakeProvider_WhenDelayIsSet_DelaysResponse()
    {
        // Arrange
        var options = new FakePaymentProviderOptions { DelayMs = 500 };
        var provider = new FakePaymentProviderClient(options);
        var request = MakeRequest();

        // Act
        var stopwatch = Stopwatch.StartNew();
        await provider.ChargeAsync(request, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(options.DelayMs);
    }

    [Fact]
    public async Task ResilientProvider_WhenInnerSucceeds_ReturnsSuccess()
    {
        // Arrange
        var inner = new FakePaymentProviderClient(new FakePaymentProviderOptions { SuccessRate = 1.0 });
        var resilient = new ResilientPaymentProviderClient(inner);
        var request = MakeRequest();

        // Act
        var result = await resilient.ChargeAsync(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ProviderChargeStatus.Succeeded);
    }

    [Fact]
    public async Task ResilientProvider_WhenInnerDeclines_ReturnsFailed()
    {
        // Arrange
        var inner = new FakePaymentProviderClient(new FakePaymentProviderOptions { AlwaysDecline = true });
        var resilient = new ResilientPaymentProviderClient(inner);
        var request = MakeRequest();

        // Act
        var result = await resilient.ChargeAsync(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ProviderChargeStatus.Failed);
    }

    [Fact]
    public async Task ResilientProvider_WhenInnerUnavailable_ReturnsUnavailable()
    {
        // Arrange
        var inner = new FakePaymentProviderClient(new FakePaymentProviderOptions { AlwaysUnavailable = true });
        var resilient = new ResilientPaymentProviderClient(inner);
        var request = MakeRequest();

        // Act
        var result = await resilient.ChargeAsync(request, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ProviderChargeStatus.Unavailable);
    }
    
    [Fact]
    public async Task ResilientProvider_WhenInnerAlwaysUnavailable_ReturnsUnavailableQuickly()
    {
        // Arrange
        var inner = new FakePaymentProviderClient(new FakePaymentProviderOptions { AlwaysUnavailable = true });
        var resilient = new ResilientPaymentProviderClient(inner);
        var request = MakeRequest();
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await resilient.ChargeAsync(request, CancellationToken.None);
        stopwatch.Stop();
        
        // Assert
        result.Status.Should().Be(ProviderChargeStatus.Unavailable);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }
    
    [Fact]
    public async Task ResilientProvider_CircuitOpens_AfterRepeatedFailures()
    {
        // Arrange
        var inner = new FakePaymentProviderClient(new FakePaymentProviderOptions { SuccessRate = 0.0 });
        var resilient = new ResilientPaymentProviderClient(inner);
        var request = MakeRequest();
        
        // Act
        for (var i = 0; i < 5; i++)
        {
            var response = await resilient.ChargeAsync(request, CancellationToken.None);
            response.Status.Should().Be(ProviderChargeStatus.Unavailable);
        }
        
        // Assert
        var stopwatch = Stopwatch.StartNew();
        var result = await resilient.ChargeAsync(request, CancellationToken.None);
        stopwatch.Stop();
        
        result.Status.Should().Be(ProviderChargeStatus.Unavailable);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMicroseconds(300));
    }
}