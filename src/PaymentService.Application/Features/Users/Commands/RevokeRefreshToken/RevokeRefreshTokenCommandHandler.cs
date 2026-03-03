using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<RevokeRefreshTokenCommandHandler> logger)
    : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling RevokeRefreshTokenCommand for refresh token: {RefreshToken}****", request.RefreshToken[..10]);
        
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == request.RefreshToken), cancellationToken);

        if (user is null)
        {
            logger.LogInformation("User not found for refresh token: {RefreshToken}****", request.RefreshToken[..10]);
            
            return Result.Failure(Error.Unauthorized("RevokeRefreshToken.Invalid", "Invalid refresh token."));
        }

        var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress);
        if (revokeResult.IsFailure) return 
            Result.Failure(revokeResult.Error);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Successfully revoked refresh token: {RefreshToken}", request.RefreshToken);
        
        return Result.Success();
    }
}