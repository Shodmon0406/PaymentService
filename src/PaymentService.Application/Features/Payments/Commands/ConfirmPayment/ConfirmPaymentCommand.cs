using PaymentService.Application.Common.Behaviors.Idempotency;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Features.Payments.Commands.ConfirmPayment;

public sealed record ConfirmPaymentCommand(Guid UserId, Guid PaymentId, string IdempotencyKey) : IIdempotentCommand<Result<PaymentResponse>>;