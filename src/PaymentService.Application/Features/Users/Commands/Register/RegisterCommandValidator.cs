using FluentValidation;

namespace PaymentService.Application.Features.Users.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage("PhoneNumber is required")
            .EmailAddress().WithMessage("Invalid email format");
        
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters")
            .EmailAddress().WithMessage("Invalid email format");
        
        RuleFor(command => command.FullName)
            .NotEmpty().WithMessage("FullName is required")
            .MaximumLength(100).WithMessage("FullName must not exceed 100 characters");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");
    }
}