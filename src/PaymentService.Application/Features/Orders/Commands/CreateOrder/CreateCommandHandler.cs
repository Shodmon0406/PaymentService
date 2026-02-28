using MediatR;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Orders.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Orders;

namespace PaymentService.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderResult = Order.Create(request.UserId, request.Amount, request.Currency);
        if (orderResult.IsFailure)
            return Result.Failure<OrderDto>(orderResult.Error);

        var order = orderResult.Value;
        dbContext.Orders.Add(order);
        await  dbContext.SaveChangesAsync(cancellationToken);
        
        return Result.Success(new OrderDto(
                order.Id,
                order.UserId,
                order.Amount,
                order.Currency,
                order.Status,
                order.CreatedAt));
    }
}