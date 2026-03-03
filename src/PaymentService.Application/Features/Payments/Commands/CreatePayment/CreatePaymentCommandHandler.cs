using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Payments;
using PaymentService.Domain.Enums.Orders;

namespace PaymentService.Application.Features.Payments.Commands.CreatePayment;

public class CreatePaymentCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<CreatePaymentCommandHandler> logger)
    : IRequestHandler<CreatePaymentCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling CreatePaymentCommand for OrderId: {OrderId}, UserId: {UserId}", command.OrderId, command.UserId);
        
        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == command.OrderId && o.UserId == command.UserId, cancellationToken);

        if (order is null)
        {
            logger.LogInformation("Order {OrderId} not found", command.OrderId);
            
            return Result.Failure<PaymentResponse>(Error.NotFound("Order.NotFound",
                $"Order '{command.OrderId}' was not found."));
        }

        if (order.Status != OrderStatus.Created)
        {
            logger.LogInformation("Order {OrderId} is in invalid status {Status} for payment initiation", command.OrderId, order.Status);
            
            return Result.Failure<PaymentResponse>(Error.Conflict("Order.Status",
                "Cannot initiate payment for an order that is not in 'Created' status."));
        }

        var paymentResult = Payment.Create(order.Id, command.UserId, order.Amount, order.Currency);
        if (paymentResult.IsFailure)
        {
            logger.LogError("Payment {PaymentId} failed to create for OrderId: {OrderId}: {Error}", 
                paymentResult.Error, command.OrderId, paymentResult.Error);
            
            return Result.Failure<PaymentResponse>(paymentResult.Error);
        }

        var payment = paymentResult.Value;
        order.AddPayment(payment);

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Payment {PaymentId} created for OrderId: {OrderId}", payment.Id, command.OrderId);

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