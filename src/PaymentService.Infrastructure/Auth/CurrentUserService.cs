using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using PaymentService.Application.Auth;

namespace PaymentService.Infrastructure.Auth;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var userIdClaim =
                httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ??
        httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value;

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string IpAddress
    {
        get
        {
            if (httpContextAccessor.HttpContext?.Request == null)
                return "unknown";

            var forwardedFor = httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            var realIp = httpContextAccessor.HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}