using MediatR;

namespace PaymentService.Application.Common.Behaviors.Idempotency;

public interface IIdempotentCommand<out TResponse> : IRequest<TResponse>
{
    Guid UserId { get; }
    string IdempotencyKey { get; }
}