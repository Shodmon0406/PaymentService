using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Auth;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IRefreshTokenGenerator refreshTokenGenerator) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber.Value == request.PhoneNumber, cancellationToken);
        
        if (user is null || !user.VerifyPassword(request.Password))
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid phone number or password."));
        
        user.RecordLogin(request.IpAddress);
        
        var rawToken = refreshTokenGenerator.Generate();
        var refreshToken = user.AddRefreshToken(rawToken, request.IpAddress);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        
        return Result.Success(new AuthResponse(accessToken, rawToken, user.Id, user.FullName));
    }
}