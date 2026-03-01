namespace PaymentService.Infrastructure.PaymentProvider;

public sealed class FakePaymentProviderOptions
{
    public const string SectionName = "FakePaymentProvider";

    public bool AlwaysUnavailable { get; set; }
    public bool AlwaysDecline { get; set; }
    public double SuccessRate { get; set; } = 1.0;
    public int DelayMs { get; set; }
}