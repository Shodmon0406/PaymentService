namespace PaymentService.Application.Features.Users.DTOs;

public sealed record CurrentUserResponse(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string? Email);