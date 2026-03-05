using FluentValidation;

namespace PaymentService.Application.Features.Users.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage("PhoneNumber is required");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}