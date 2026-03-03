using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Payments.Queries.GetPaymentsByOrder;

public sealed class GetPaymentsByOrderQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetPaymentsByOrderQueryHandler> logger)
    : IRequestHandler<GetPaymentsByOrderQuery, Result<IReadOnlyList<PaymentResponse>>>
{
    public async Task<Result<IReadOnlyList<PaymentResponse>>> Handle(GetPaymentsByOrderQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetPaymentsByOrderQuery for OrderId: {OrderId}, UserId: {UserId}", request.OrderId, request.UserId);
        
        var orderExists = await dbContext.Orders
            .AnyAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, cancellationToken);

        if (!orderExists)
        {
            logger.LogInformation("Order {OrderId} not found for UserId: {UserId}", request.OrderId, request.UserId);
            
            return Result.Failure<IReadOnlyList<PaymentResponse>>(Error.NotFound("Order.NotFound",
                $"Order '{request.OrderId}' was not found."));
        }

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == request.OrderId && p.UserId == request.UserId)
            .OrderByDescending(p => p.Id)
            .Select(p => new PaymentResponse(
                p.Id,
                p.OrderId,
                p.UserId,
                p.Amount,
                p.Currency,
                p.Status,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Found {Count} payments for OrderId: {OrderId}, UserId: {UserId}", payments.Count, request.OrderId, request.UserId);

        return Result.Success<IReadOnlyList<PaymentResponse>>(payments);
    }
}