using MediatR;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.RevokeRefreshToken;

public record RevokeRefreshTokenCommand(
    string RefreshToken,
    string IpAddress) : IRequest<Result>;