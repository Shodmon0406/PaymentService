using MediatR;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.Login;

public sealed record LoginCommand(
    string PhoneNumber,
    string Password,
    string IpAddress) : IRequest<Result<AuthResponse>>;