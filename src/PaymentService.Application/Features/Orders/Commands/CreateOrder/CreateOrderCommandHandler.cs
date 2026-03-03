using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Orders.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Orders;

namespace PaymentService.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating order for user {UserId} with amount {Amount} {Currency}",
            request.UserId, request.Amount, request.Currency);
        
        var orderResult = Order.Create(request.UserId, request.Amount, request.Currency);
        if (orderResult.IsFailure)
        {
            logger.LogWarning("Failed to create order for user {UserId}: {Error}", request.UserId, orderResult.Error);
            return Result.Failure<OrderResponse>(orderResult.Error);
        }

        var order = orderResult.Value;
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, request.UserId);

        return Result.Success(new OrderResponse(
            order.Id,
            order.UserId,
            order.Amount,
            order.Currency,
            order.Status,
            order.CreatedAt));
    }
}