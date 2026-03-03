using FluentValidation;

namespace PaymentService.Application.Features.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("UserId is required");
        
        RuleFor(command => command.OrderId)
            .NotEmpty().WithMessage("OrderId is required");
        
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required");
    }
}