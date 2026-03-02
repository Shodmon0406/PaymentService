using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Auth;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Idempotency;

namespace PaymentService.Application.Common.Behaviors.Idempotency;

public class IdempotencyBehavior<TRequest, TResponse>(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not IIdempotentCommand<TResponse> idempotentRequest)
        {
            return await next(ct);
        }

        var key = idempotentRequest.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new IdempotencyKeyMissingException();
        }

        var userId = currentUserService.UserId;
        if (userId is null)
        {
            throw new InvalidOperationException("User must be authenticated to use idempotency.");
        }

        var existing = await dbContext.IdempotencyKeys
            .FirstOrDefaultAsync(ik => ik.UserId == userId.Value && ik.Key == key, ct);

        if (existing is not null)
        {
            if (existing.ExpiresAt < DateTimeOffset.UtcNow)
            {
                dbContext.IdempotencyKeys.Remove(existing);
                await dbContext.SaveChangesAsync(ct);
            }
            else
            {
                logger.LogInformation("Idempotent response reused for key {Key}", key);

                return JsonSerializer.Deserialize<TResponse>(existing.ResponseBody)
                       ?? throw new InvalidOperationException("Cannot deserialize saved response");
            }
        }

        var response = await next(ct);

        var responseBody = JsonSerializer.Serialize(response);
        var statusCode = GetStatusCode(response);

        var entry = IdempotencyKey.Create(
            userId.Value,
            key,
            ComputeRequestHash(request),
            statusCode,
            responseBody,
            DateTimeOffset.UtcNow.AddHours(24)
        );

        await dbContext.IdempotencyKeys.AddAsync(entry, ct);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            logger.LogWarning("Idempotency key saving failed");
            
            var existingKey = dbContext.IdempotencyKeys
                .FirstOrDefault(ik => ik.UserId == userId.Value && ik.Key == key);

            if (existingKey is not null)
                return JsonSerializer.Deserialize<TResponse>(existingKey.ResponseBody)
                       ?? throw new InvalidOperationException("Cannot deserialize saved response");
        }

        return response;
    }

    private static string ComputeRequestHash(TRequest req)
    {
        var hashBytes =
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(req)));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static int GetStatusCode(TResponse response)
    {
        if (response.IsSuccess)
        {
            return 200;
        }

        return response.Error.Type switch
        {
            ErrorType.None => 200,
            ErrorType.Validation => 400,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden => 403,
            ErrorType.Failure => 500,
            ErrorType.ServiceUnavailable => 503,
            ErrorType.PaymentFailed => 402,
            _ => 500
        };
    }
}