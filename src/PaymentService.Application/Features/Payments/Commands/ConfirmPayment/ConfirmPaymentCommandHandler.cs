using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Application.Features.Services;
using PaymentService.Domain.Common;
using PaymentService.Domain.Enums.Payments;

namespace PaymentService.Application.Features.Payments.Commands.ConfirmPayment;

public sealed class ConfirmPaymentCommandHandler(IApplicationDbContext dbContext, IOrderLockService orderLockService)
    : IRequestHandler<ConfirmPaymentCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        var payment = await dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.UserId == request.UserId, cancellationToken);

        if (payment is null)
            return Result.Failure<PaymentResponse>(Error.NotFound("Payment.NotFound",
                $"Payment {request.PaymentId} was not found"));
        
        await orderLockService.AcquireLockAsync(payment.OrderId, cancellationToken);
        
        var freshPayment = await dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);
        
        if (freshPayment is null)
            return Result.Failure<PaymentResponse>(Error.NotFound("Payment.NotFound",
                $"Payment {request.PaymentId} was not found"));

        if (freshPayment.Status != PaymentStatus.Pending)
            return Result.Failure<PaymentResponse>(
                Error.Conflict("Payment.Status", "The payment is no longer in Pending status."));

        var hasSuccessfulPayment = await dbContext.Payments
            .AnyAsync(p => p.OrderId == freshPayment.OrderId && p.Status == PaymentStatus.Successful, cancellationToken);

        if (hasSuccessfulPayment)
            return Result.Failure<PaymentResponse>(
                Error.Conflict("Payment.AlreadyConfirmed", "Another payment for this order has already been confirmed."));

        var confirmResult = freshPayment.MarkAsCompleted();
        if (confirmResult.IsFailure)
            return Result.Failure<PaymentResponse>(confirmResult.Error);

        var orderResult = freshPayment.Order.MarkAsPaid();
        if (orderResult.IsFailure)
            return Result.Failure<PaymentResponse>(orderResult.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
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