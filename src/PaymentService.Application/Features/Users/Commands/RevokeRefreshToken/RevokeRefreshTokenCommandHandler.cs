using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == request.RefreshToken), cancellationToken);

        if (user is null)
            return Result.Failure(Error.Unauthorized("RevokeRefreshToken.Invalid", "Invalid refresh token."));

        var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress);
        if (revokeResult.IsFailure) return 
            Result.Failure(revokeResult.Error);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}