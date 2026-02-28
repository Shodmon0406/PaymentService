using MediatR;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress) : IRequest<Result<AuthResponse>>;