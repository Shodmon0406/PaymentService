using FluentValidation;

namespace PaymentService.Application.Features.Payments.Commands.ConfirmPayment;

public sealed class ConfirmPaymentCommandValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("UserId is required");
        
        RuleFor(command => command.PaymentId)
            .NotEmpty().WithMessage("PaymentId is required");
        
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required");
    }
}