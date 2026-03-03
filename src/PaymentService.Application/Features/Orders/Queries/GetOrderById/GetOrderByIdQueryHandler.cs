using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Orders.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetOrderByIdQueryHandler> logger)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetOrderByIdQuery for OrderId: {OrderId}, UserId: {UserId}", request.OrderId, request.UserId);
        
        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order not found for OrderId: {OrderId}", request.OrderId);
            
            return Result.Failure<OrderResponse>(Error.NotFound("Order.NotFound", "Order not found."));
        }
        
        logger.LogInformation("Handling GetOrderByIdQuery for UserId: {UserId}", request.UserId);

        return Result.Success(new OrderResponse(
            order.Id,
            order.UserId,
            order.Amount,
            order.Currency,
            order.Status,
            order.CreatedAt));
    }
}