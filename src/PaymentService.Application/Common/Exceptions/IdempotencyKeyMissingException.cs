namespace PaymentService.Application.Common.Exceptions;

public class IdempotencyKeyMissingException : Exception
{
    public IdempotencyKeyMissingException() : base("Idempotency key is missing.") { }

    public IdempotencyKeyMissingException(string message) : base(message) { }

    public IdempotencyKeyMissingException(string message, Exception innerException) : base(message, innerException) { }
}