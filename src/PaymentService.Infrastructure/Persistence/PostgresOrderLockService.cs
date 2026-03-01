using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Features.Services;

namespace PaymentService.Infrastructure.Persistence;

public sealed class PostgresOrderLockService(ApplicationDbContext dbContext) : IOrderLockService
{
    public Task AcquireLockAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM orders WHERE id = {orderId} FOR UPDATE", cancellationToken);
}