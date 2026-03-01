namespace PaymentService.Infrastructure.PaymentProvider;

public class ProviderUnavailableException : Exception
{
    public ProviderUnavailableException(string msg) : base(msg) { }
    
    public ProviderUnavailableException(string msg, Exception inner) : base(msg, inner) { }
}