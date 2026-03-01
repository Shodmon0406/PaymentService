using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Auth;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.ValueObjects;

namespace PaymentService.Application.Features.Users.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IRefreshTokenGenerator refreshTokenGenerator) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        if (phoneNumber.IsFailure)
            return Result.Failure<AuthResponse>(Error.Validation("Auth.InvalidPhoneNumber", "The provided phone number is invalid."));
        
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.PhoneNumber.Value == phoneNumber.Value, cancellationToken);
        
        if (user is null || !user.VerifyPassword(request.Password))
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid phone number or password."));
        
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        
        user.RecordLogin(request.IpAddress);
        
        var rawToken = refreshTokenGenerator.Generate();
        var refreshToken = user.AddRefreshToken(rawToken, request.IpAddress);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var accessToken = jwtTokenService.GenerateAccessToken(user, roles);
        
        return Result.Success(new AuthResponse(accessToken, rawToken, user.Id, user.FullName));
    }
}