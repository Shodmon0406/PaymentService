using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Application.Features.Services;
using PaymentService.Domain.Common;
using PaymentService.Domain.Enums.Payments;

namespace PaymentService.Application.Features.Payments.Commands.ConfirmPayment;

public sealed class ConfirmPaymentCommandHandler(
    IApplicationDbContext dbContext,
    IOrderLockService orderLockService,
    IPaymentProviderClient paymentProviderClient,
    ILogger<ConfirmPaymentCommandHandler> logger)
    : IRequestHandler<ConfirmPaymentCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling ConfirmPaymentCommand for PaymentId: {PaymentId}, UserId: {UserId}", request.PaymentId, request.UserId);
        
        var payment = await dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.UserId == request.UserId, cancellationToken);

        if (payment is null)
        {
            logger.LogInformation("Payment {PaymentId} not found", request.PaymentId);
            
            return Result.Failure<PaymentResponse>(Error.NotFound("Payment.NotFound",
                $"Payment {request.PaymentId} was not found"));
        }

        var chargeRequest = new ProviderChargeRequest(payment.Id, payment.Amount, payment.Currency);

        var chargeResult = await paymentProviderClient.ChargeAsync(chargeRequest, cancellationToken);

        if (chargeResult.Status != ProviderChargeStatus.Succeeded)
        {
            logger.LogError("Payment {PaymentId} failed to charge", request.PaymentId);
            
            payment.MarkAsFailed();
            await dbContext.SaveChangesAsync(cancellationToken);

            return chargeResult.Status switch
            {
                ProviderChargeStatus.Failed => Result.Failure<PaymentResponse>(Error.PaymentFailed("Payment.Declined",
                    chargeResult.ErrorMessage ?? "Payment was declined by provider")),
                ProviderChargeStatus.Unavailable => Result.Failure<PaymentResponse>(Error.PaymentFailed("Payment.ProviderUnavailable",
                    chargeResult.ErrorMessage ?? "Payment provider is currently unavailable")),
                _ => Result.Failure<PaymentResponse>(Error.PaymentFailed("Payment.ProviderFailed",
                    chargeResult.ErrorMessage ?? "Payment failed at provider"))
            };
        }
        
        logger.LogInformation("Payment {PaymentId} successfully charged", request.PaymentId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await orderLockService.AcquireLockAsync(payment.OrderId, cancellationToken);
        
        logger.LogInformation("Payment {PaymentId} acquired lock for Order {OrderId}", request.PaymentId, payment.OrderId);

        var freshPayment = await dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (freshPayment is null)
        {
            logger.LogError("Payment {PaymentId} not found after acquiring lock", request.PaymentId);
            
            return Result.Failure<PaymentResponse>(Error.NotFound("Payment.NotFound",
                $"Payment {request.PaymentId} was not found"));
        }

        if (freshPayment.Status != PaymentStatus.Pending)
        {
            logger.LogInformation("Payment {PaymentId} is not in Pending status", request.PaymentId);
            
            return Result.Failure<PaymentResponse>(Error.Conflict("Payment.Status",
                "The payment is no longer in Pending status."));
        }

        var hasSuccessfulPayment = await dbContext.Payments
            .AnyAsync(p => p.OrderId == freshPayment.OrderId && p.Status == PaymentStatus.Successful,
                cancellationToken);

        if (hasSuccessfulPayment)
        {
            logger.LogInformation("Payment {PaymentId} cannot be confirmed because another payment for the same order is already confirmed", request.PaymentId);
            
            return Result.Failure<PaymentResponse>(Error.Conflict("Payment.AlreadyConfirmed",
                "Another payment for this order has already been confirmed."));
        }

        var confirmResult = freshPayment.MarkAsCompleted();
        if (confirmResult.IsFailure)
        {
            logger.LogError("Failed to mark Payment {PaymentId} as completed: {Error}", request.PaymentId, confirmResult.Error);
            
            return Result.Failure<PaymentResponse>(confirmResult.Error);
        }

        var orderResult = freshPayment.Order.MarkAsPaid();
        if (orderResult.IsFailure)
        {
            logger.LogError("Failed to mark Order {OrderId} as paid for Payment {PaymentId}: {Error}", freshPayment.OrderId, request.PaymentId, orderResult.Error);
            
            return Result.Failure<PaymentResponse>(orderResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        logger.LogInformation("Payment {PaymentId} confirmed and Order {OrderId} marked as paid", request.PaymentId, freshPayment.OrderId);

        var response = new PaymentResponse(
            freshPayment.Id,
            freshPayment.OrderId,
            freshPayment.UserId,
            freshPayment.Amount,
            freshPayment.Currency,
            freshPayment.Status,
            freshPayment.CreatedAt);

        return Result.Success(response);
    }
}