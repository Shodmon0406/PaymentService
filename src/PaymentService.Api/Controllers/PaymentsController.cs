using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PaymentService.Api.Common.RateLimiting;
using PaymentService.Api.Common.Result;
using PaymentService.Application.Auth;
using PaymentService.Application.Features.Payments.Commands.ConfirmPayment;
using PaymentService.Application.Features.Payments.Commands.CreatePayment;
using PaymentService.Application.Features.Payments.DTOs;
using PaymentService.Application.Features.Payments.Queries.GetPaymentsByOrder;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController(ISender sender, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("{orderId:guid}")]
    [Authorize("RequireUserRole")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePayment(
        [FromRoute] Guid orderId,
        [FromHeader(Name = "Idempotency-key"), Required] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        var command = new CreatePaymentCommand(userId.Value, orderId, idempotencyKey);
        var result = await sender.Send(command, cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{paymentId:guid}/confirm")]
    [Authorize("RequireUserRole")]
    [EnableRateLimiting(RateLimitingExtensions.PolicyNames.PaymentConfirm)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmPayment(
        [FromRoute] Guid paymentId, 
        [FromHeader(Name = "Idempotency-key"), Required] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var command = new ConfirmPaymentCommand(userId.Value, paymentId, idempotencyKey);
        var result = await sender.Send(command, cancellationToken);

        return result.ToActionResult();
    }
}