namespace PaymentService.Application.Auth;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    string IpAddress { get; }
}