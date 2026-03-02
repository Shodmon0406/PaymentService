using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.Enums.Orders;

namespace PaymentService.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreatePaymentCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == command.OrderId && o.UserId == command.UserId, cancellationToken);

        if (order is null)
            return Result.Failure<PaymentResponse>(Error.NotFound("Order.NotFound",
                $"Order '{command.OrderId}' was not found."));

        if (order.Status != OrderStatus.Created)
            return Result.Failure<PaymentResponse>(Error.Conflict("Order.Status",
                "Cannot initiate payment for an order that is not in 'Created' status."));
        
        var paymentResult = Domain.Entities.Payments.Payment.Create(order.Id, command.UserId, order.Amount, order.Currency);
        if (paymentResult.IsFailure)
            return Result.Failure<PaymentResponse>(paymentResult.Error);
        
        var payment = paymentResult.Value;
        order.AddPayment(payment);
        
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return Result.Success(new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.UserId,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.CreatedAt));
    }
}