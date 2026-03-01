using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.Enums.Payments;

namespace PaymentService.Application.Features.Payments.Commands.ConfirmPayment;

public sealed class ConfirmPaymentCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ConfirmPaymentCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.UserId == request.UserId, cancellationToken);

        if (payment is null)
            return Result.Failure<PaymentResponse>(Error.NotFound("Payment.NotFound",
                $"Payment {request.PaymentId} was not found"));


        if (payment.Status != PaymentStatus.Pending)
            return Result.Failure<PaymentResponse>(
                Error.Conflict("Payment.Status", "The payment is no longer in Pending status."));

        var hasSuccessfulPayment = await dbContext.Payments
            .AnyAsync(p => p.OrderId == payment.OrderId && p.Status == PaymentStatus.Successful, cancellationToken);

        if (hasSuccessfulPayment)
            return Result.Failure<PaymentResponse>(
                Error.Conflict("Payment.AlreadyConfirmed", "Another payment for this order has already been confirmed."));

        var confirmResult = payment.MarkAsCompleted();
        if (confirmResult.IsFailure)
            return Result.Failure<PaymentResponse>(confirmResult.Error);

        var orderResult = payment.Order.MarkAsPaid();
        if (orderResult.IsFailure)
            return Result.Failure<PaymentResponse>(orderResult.Error);

        await dbContext.SaveChangesAsync(cancellationToken);
        
        var response = new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.UserId,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.CreatedAt);
        
        return Result.Success(response);
    }
}