using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Features.Auth.Commands;
namespace HotelBilling.API.Controllers;

/// <summary>Authentication — login, register, token refresh, logout</summary>
public class AuthController(IMediator mediator, ICurrentUserService currentUser) : BaseController(mediator)
{
    /// <summary>Login with email and password. Returns JWT access token + refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
        => OkResult(await Mediator.Send(command, ct), "Login successful");

    /// <summary>Register a new user account (Admin only).</summary>
    [HttpPost("register")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(201)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
        => CreatedResult(await Mediator.Send(command, ct), "User registered successfully");

    /// <summary>Refresh expired access token using a valid refresh token.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
        => OkResult(await Mediator.Send(command, ct), "Token refreshed");


    /// <summary>Change password for the authenticated user.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        var cmd = command with { UserId = currentUser.UserId ?? 0 };
        return OkResult(await Mediator.Send(cmd, ct), "Password changed successfully");
    }

    /// <summary>Logout — invalidates the refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? 0;
        return OkResult(await Mediator.Send(new LogoutCommand(userId), ct), "Logged out");
    }

    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(200)]
    public IActionResult Me() => OkResult(new
    {
        currentUser.UserId,
        currentUser.Email,
        currentUser.Role,
        currentUser.IsAuthenticated
    });
}
