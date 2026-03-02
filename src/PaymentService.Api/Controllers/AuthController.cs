using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PaymentService.Api.Common.RateLimiting;
using PaymentService.Api.Common.Result;
using PaymentService.Application.Auth;
using PaymentService.Application.Features.Users.Commands.Login;
using PaymentService.Application.Features.Users.Commands.RefreshToken;
using PaymentService.Application.Features.Users.Commands.Register;
using PaymentService.Application.Features.Users.Commands.RevokeRefreshToken;
using PaymentService.Application.Features.Users.DTOs;
using PaymentService.Application.Features.Users.Queries.GetCurrentUser;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimitingExtensions.PolicyNames.Auth)]
public sealed class AuthController(ISender sender, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.PhoneNumber,
            request.Email,
            request.FullName,
            request.Password,
            currentUserService.IpAddress);

        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.PhoneNumber,
            request.Password,
            currentUserService.IpAddress);

        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(
            request.RefreshToken,
            currentUserService.IpAddress);

        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }
    
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken(
        [FromBody] RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RevokeRefreshTokenCommand(
            request.RefreshToken,
            currentUserService.IpAddress);
        
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    [Authorize("RequireUserRole")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        
        var query = new GetCurrentUserQuery(userId!.Value);
        var result = await sender.Send(query, cancellationToken);
        
        return result.ToActionResult();
    }
}