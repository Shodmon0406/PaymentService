using MediatR;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.Register;

public sealed record RegisterCommand(
    string PhoneNumber,
    string Email,
    string FullName,
    string Password,
    string IpAddress) : IRequest<Result<AuthResponse>>;