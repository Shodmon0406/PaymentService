using PaymentService.Domain.Entities.Users;

namespace PaymentService.Application.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
}