using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Orders.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, cancellationToken);

        if (order is null)
            return Result.Failure<OrderDto>(Error.NotFound("Order.NotFound", "Order not found."));

        return Result.Success(new OrderDto(
            order.Id,
            order.UserId,
            order.Amount,
            order.Currency,
            order.Status,
            order.CreatedAt));
    }
}