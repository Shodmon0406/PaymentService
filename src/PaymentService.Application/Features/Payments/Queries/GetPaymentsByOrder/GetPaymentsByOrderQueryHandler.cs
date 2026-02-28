using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Payments.Queries.GetPaymentsByOrder;

public sealed class GetPaymentsByOrderQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetPaymentsByOrderQuery, Result<IReadOnlyList<PaymentDto>>>
{
    public async Task<Result<IReadOnlyList<PaymentDto>>> Handle(GetPaymentsByOrderQuery request,
        CancellationToken cancellationToken)
    {
        var orderExists = await dbContext.Orders
            .AnyAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, cancellationToken);

        if (!orderExists)
            return Result.Failure<IReadOnlyList<PaymentDto>>(Error.NotFound("Order.NotFound",
                $"Order '{request.OrderId}' was not found."));

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == request.OrderId && p.UserId == request.UserId)
            .OrderByDescending(p => p.Id)
            .Select(p => new PaymentDto(
                p.Id,
                p.OrderId,
                p.UserId,
                p.Amount,
                p.Currency,
                p.Status,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PaymentDto>>(payments);
    }
}