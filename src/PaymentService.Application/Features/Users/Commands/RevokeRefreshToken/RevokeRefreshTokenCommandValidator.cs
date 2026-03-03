using FluentValidation;

namespace PaymentService.Application.Features.Users.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}