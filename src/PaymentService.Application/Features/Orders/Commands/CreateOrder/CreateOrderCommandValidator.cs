using FluentValidation;

namespace PaymentService.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(command => command.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be 3 characters");

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}