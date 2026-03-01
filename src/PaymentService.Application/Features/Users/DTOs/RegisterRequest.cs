namespace PaymentService.Application.Features.Users.DTOs;

public sealed record RegisterRequest(
    string PhoneNumber,
    string Email,
    string FullName,
    string Password);