using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Auth;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Users;

namespace PaymentService.Application.Features.Users.Commands.Register;

public sealed class RegisterCommandHandler(
    IApplicationDbContext dbContext, 
    IJwtTokenService tokenService,
    IRefreshTokenGenerator refreshTokenGenerator) 
    : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var phoneExists = await dbContext.Users.AnyAsync(u => u.PhoneNumber.Value == request.PhoneNumber, cancellationToken);
        if (phoneExists)
            return Result.Failure<AuthResponse>(Error.Conflict("User.PhoneExists", "A user with this phone number already exists."));
        
        var emailExists = await dbContext.Users.AnyAsync(u => u.Email != null && u.Email.Value == request.Email, cancellationToken);
        if (emailExists)
            return Result.Failure<AuthResponse>(Error.Conflict("User.EmailExists", "A user with this email already exists."));
        
        var userResult = User.Register(request.PhoneNumber, request.Email, request.FullName, request.Password);
        if (userResult.IsFailure)
            return Result.Failure<AuthResponse>(userResult.Error);
        
        var user = userResult.Value;
        dbContext.Users.Add(user);
        
        var rawToken = refreshTokenGenerator.Generate();
        var refreshToken = user.AddRefreshToken(rawToken, request.IpAddress);
        dbContext.RefreshTokens.Add(refreshToken);
        
        var defaultRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == Role.Names.User, cancellationToken);
        
        if (defaultRole != null)
        {
            var userRole = UserRole.Create(user.Id, defaultRole.Id);
            dbContext.UserRoles.Add(userRole);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        var accessToken = tokenService.GenerateAccessToken(user, [Role.Names.User]);
        
        return Result.Success(new AuthResponse(accessToken, rawToken, user.Id, user.FullName));
    }
}