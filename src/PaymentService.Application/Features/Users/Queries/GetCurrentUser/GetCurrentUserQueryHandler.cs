using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (user is null)
            return Result.Failure<CurrentUserResponse>(Error.NotFound("User.NotFound", "User not found."));
        
        var dto = new CurrentUserResponse(user.Id, user.FullName, user.PhoneNumber, user.Email);
        
        return Result.Success(dto);
    }
}