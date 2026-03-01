namespace PaymentService.Application.Features.Users.DTOs;

public sealed record LoginRequest(
    string PhoneNumber,
    string Password);