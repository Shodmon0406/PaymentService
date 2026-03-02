using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.Common.Result;
using PaymentService.Application.Auth;
using PaymentService.Application.Features.Orders.Commands.CreateOrder;
using PaymentService.Application.Features.Orders.DTOs;
using PaymentService.Application.Features.Orders.Queries.GetOrderById;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(ISender sender, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    [Authorize("RequireUserRole")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request, 
        [FromHeader(Name = "Idempotency-key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        var command = new CreateOrderCommand(userId.Value, request.Amount, request.Currency, idempotencyKey);
        var result = await sender.Send(command, cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [Authorize("RequireUserRole")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) 
            return Unauthorized();

        var query = new GetOrderByIdQuery(id, userId.Value);
        var result = await sender.Send(query, cancellationToken);

        return result.ToActionResult();
    }
}