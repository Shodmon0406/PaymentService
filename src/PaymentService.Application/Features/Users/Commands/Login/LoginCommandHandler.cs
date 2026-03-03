using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Auth;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.ValueObjects;

namespace PaymentService.Application.Features.Users.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IRefreshTokenGenerator refreshTokenGenerator,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling LoginCommand for PhoneNumber: {PhoneNumber}", request.PhoneNumber);
        
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        if (phoneNumber.IsFailure)
        {
            logger.LogInformation("Invalid phone number: {PhoneNumber}", phoneNumber.Value);
            
            return Result.Failure<AuthResponse>(Error.Validation("Auth.InvalidPhoneNumber",
                "The provided phone number is invalid."));
        }
        
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.PhoneNumber.Value == phoneNumber.Value, cancellationToken);
        
        if (user is null || !user.VerifyPassword(request.Password))
        {
            logger.LogInformation("Invalid login attempt for PhoneNumber: {PhoneNumber}", request.PhoneNumber);
            
            return Result.Failure<AuthResponse>(Error.Unauthorized("Auth.InvalidCredentials",
                "Invalid phone number or password."));
        }
        
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        
        user.RecordLogin(request.IpAddress);
        
        var rawToken = refreshTokenGenerator.Generate();
        var refreshToken = user.AddRefreshToken(rawToken, request.IpAddress);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var accessToken = jwtTokenService.GenerateAccessToken(user, roles);
        
        logger.LogInformation("User {UserId} logged in successfully", user.Id);
        
        return Result.Success(new AuthResponse(accessToken, rawToken, user.Id, user.FullName));
    }
}