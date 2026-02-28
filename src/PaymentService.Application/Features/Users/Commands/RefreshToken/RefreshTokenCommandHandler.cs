using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Auth;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IRefreshTokenGenerator refreshTokenGenerator) : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == request.RefreshToken), cancellationToken);

        if (user is null)
            return Result.Failure<AuthResponse>(Error.Unauthorized("RefreshToken.Invalid", "Invalid refresh token."));
        
        var newRawToken = refreshTokenGenerator.Generate();
        
        var revokeResult = user.RevokeRefreshToken(request.RefreshToken, request.IpAddress, newRawToken);
        if (revokeResult.IsFailure)
            return Result.Failure<AuthResponse>(revokeResult.Error);
        
        var newRefreshToken = user.AddRefreshToken(newRawToken, request.IpAddress);
        dbContext.RefreshTokens.Add(newRefreshToken);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        
        return Result.Success(new AuthResponse(accessToken, newRawToken, user.Id, user.FullName));
    }
}