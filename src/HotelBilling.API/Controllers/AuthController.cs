using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Features.Auth.Commands;

namespace HotelBilling.API.Controllers;

/// <summary>Authentication - login, register, token refresh, logout</summary>
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
    [AllowAnonymous]
    //[Authorize(Policy = "AdminOnly")]
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
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user id claim is missing.");

        var cmd = command with { UserId = userId };
        return OkResult(await Mediator.Send(cmd, ct), "Password changed successfully");
    }

    /// <summary>Logout - invalidates the refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user id claim is missing.");

        return OkResult(await Mediator.Send(new LogoutCommand(userId), ct), "Logged out");
    }

    /// <summary>Initiate Two-Factor Authentication (2FA) setup. Returns QR Code and Manual Setup Key.</summary>
    [HttpPost("2fa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(Enable2FAResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Enable2Fa(CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user id claim is missing.");

        return OkResult(await Mediator.Send(new Enable2FACommand(userId), ct), "2FA setup initiated");
    }

    /// <summary>Confirm and activate 2FA by verifying the first OTP. Returns recovery codes.</summary>
    [HttpPost("2fa/verify")]
    [Authorize]
    [ProducesResponseType(typeof(Verify2FAResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Verify2Fa([FromBody] Verify2FARequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user id claim is missing.");

        var command = new Verify2FACommand(userId, request.Otp);
        return OkResult(await Mediator.Send(command, ct), "2FA enabled successfully");
    }

    /// <summary>Disable Two-Factor Authentication (2FA).</summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Disable2Fa([FromBody] Disable2FARequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user id claim is missing.");

        var command = new Disable2FACommand(userId, request.Password, request.Otp);
        return OkResult(await Mediator.Send(command, ct), "2FA disabled successfully");
    }

    /// <summary>Regenerate a new set of 2FA backup recovery codes.</summary>
    [HttpPost("2fa/generate-recovery-codes")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GenerateRecoveryCodes(CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user id claim is missing.");

        return OkResult(await Mediator.Send(new GenerateRecoveryCodesCommand(userId), ct), "Recovery codes regenerated");
    }

    /// <summary>Verify login OTP or recovery code to complete the authentication process.</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyLoginOtpCommand command, CancellationToken ct)
        => OkResult(await Mediator.Send(command, ct), "OTP verified successfully");

    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Me([FromServices] IUserRepository users, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("Authenticated user id claim is missing.");

        var user = await users.GetByIdAsync(userId, ct);

        return OkResult(new
        {
            currentUser.UserId,
            currentUser.Email,
            currentUser.Role,
            currentUser.IsAuthenticated,
            TwoFactorEnabled = user?.TwoFactorEnabled ?? false
        });
    }
}

/// <summary>DTO representing verification request for 2FA confirmation.</summary>
public record Verify2FARequest(string Otp);

/// <summary>DTO representing disabling request for 2FA.</summary>
public record Disable2FARequest(string Password, string Otp);
