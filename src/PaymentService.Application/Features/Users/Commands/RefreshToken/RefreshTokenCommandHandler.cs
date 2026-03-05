using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Auth;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IRefreshTokenGenerator refreshTokenGenerator,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling RefreshTokenCommand for refresh token");
        
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == request.RefreshToken), cancellationToken);

        if (user is null)
        {
            logger.LogInformation("User not found for refresh token");
            
            return Result.Failure<AuthResponse>(Error.Validation("RefreshToken.Invalid", "Invalid refresh token."));
        }
        
        var newRawToken = refreshTokenGenerator.Generate();
        
        var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress, newRawToken);
        if (revokeResult.IsFailure)
        {
            logger.LogInformation("Failed to revoke refresh token. Reason: {Reason}", revokeResult.Error.Message);
            
            return Result.Failure<AuthResponse>(revokeResult.Error);
        }
        
        var newRefreshToken = user.AddRefreshToken(newRawToken, request.IpAddress);
        dbContext.RefreshTokens.Add(newRefreshToken);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
        
        var accessToken = jwtTokenService.GenerateAccessToken(user, roles);
        
        logger.LogInformation("Successfully refreshed token for user {UserId}. New access token generated", user.Id);
        
        return Result.Success(new AuthResponse(accessToken, newRawToken, user.Id, user.FullName));
    }
}