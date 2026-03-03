using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetCurrentUserQueryHandler> logger) 
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetCurrentUserQuery for user ID: {UserId}", request.UserId);
        
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (user is null)
        {
            logger.LogInformation("User with ID {UserId} not found", request.UserId);
            
            return Result.Failure<CurrentUserResponse>(Error.NotFound("User.NotFound", "User not found."));
        }
        
        var dto = new CurrentUserResponse(user.Id, user.FullName, user.PhoneNumber, user.Email);
        
        logger.LogInformation("Successfully retrieved user with ID {UserId}", request.UserId);
        
        return Result.Success(dto);
    }
}