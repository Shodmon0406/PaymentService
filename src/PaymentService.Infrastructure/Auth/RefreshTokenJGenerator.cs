using System.Security.Cryptography;
using PaymentService.Application.Auth;

namespace PaymentService.Infrastructure.Auth;

public sealed class RefreshTokenJGenerator : IRefreshTokenGenerator
{
    private const int RefreshTokenLength = 64;
    
    public string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(RefreshTokenLength);
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}