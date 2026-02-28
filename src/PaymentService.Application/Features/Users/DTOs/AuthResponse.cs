namespace PaymentService.Application.Features.Users.DTOs;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string FullName);