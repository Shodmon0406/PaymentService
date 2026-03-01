namespace PaymentService.Application.Features.Services;

public interface IOrderLockService
{
    Task AcquireLockAsync(Guid orderId, CancellationToken cancellationToken);
}