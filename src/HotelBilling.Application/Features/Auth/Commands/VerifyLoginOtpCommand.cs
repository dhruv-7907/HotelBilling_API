using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;

namespace HotelBilling.Application.Features.Auth.Commands;

/// <summary>
/// Command to complete login by verifying the OTP code or recovery code.
/// </summary>
public record VerifyLoginOtpCommand(int UserId, string Code) : IRequest<AuthResponse>;

/// <summary>
/// Validator for VerifyLoginOtpCommand.
/// </summary>
public class VerifyLoginOtpCommandValidator : AbstractValidator<VerifyLoginOtpCommand>
{
    public VerifyLoginOtpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Valid user identifier is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .MinimumLength(6).WithMessage("Verification code must be at least 6 characters.")
            .MaximumLength(20).WithMessage("Verification code must not exceed 20 characters.");
    }
}

/// <summary>
/// Handler for verifying OTP or recovery code logins and generating authentication tokens.
/// </summary>
public class VerifyLoginOtpCommandHandler(
    IUserRepository users,
    ITwoFactorService twoFactor,
    IJwtService jwt) : IRequestHandler<VerifyLoginOtpCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(VerifyLoginOtpCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct)
            ?? throw new UnauthorizedException("Invalid user or authentication request.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Account is inactive.");
        }

        if (!user.TwoFactorEnabled)
        {
            throw new ConflictException("Two-factor authentication is not enabled on this account.");
        }

        // Check OTP Lockout
        if (user.OtpLockedUntil.HasValue && user.OtpLockedUntil.Value > DateTime.UtcNow)
        {
            var remainingTime = user.OtpLockedUntil.Value - DateTime.UtcNow;
            throw new UnauthorizedException($"Account is temporarily locked due to too many failed OTP attempts. Try again in {Math.Ceiling(remainingTime.TotalMinutes)} minutes.");
        }

        var inputCode = request.Code.Trim();

        // 1. Attempt recovery code check if the code has a recovery code format (e.g. contains hyphen or length > 6)
        var isRecoveryCode = inputCode.Contains('-') || inputCode.Length > 6;
        if (isRecoveryCode)
        {
            var normalizedRecoveryCode = inputCode.ToUpperInvariant();
            var codeBytes = Encoding.UTF8.GetBytes(normalizedRecoveryCode);
            var hashBase64 = Convert.ToBase64String(SHA256.HashData(codeBytes));

            var recoveryCodes = await users.GetRecoveryCodesAsync(user.Id, ct);
            var matchingCode = recoveryCodes.FirstOrDefault(c => c.CodeHash == hashBase64 && !c.Used);

            if (matchingCode != null)
            {
                // Mark recovery code as used
                await users.MarkRecoveryCodeAsUsedAsync(user.Id, hashBase64, ct);

                // Reset failed attempts and lockout
                await users.UpdateOtpAttemptsAsync(user.Id, 0, null, ct);

                // Generate Auth Tokens
                var accessToken = jwt.GenerateAccessToken(user);
                var refreshToken = jwt.GenerateRefreshToken();
                var expiry = DateTime.UtcNow.AddDays(7);

                await users.UpdateRefreshTokenAsync(user.Id, refreshToken, expiry, ct);
                await users.UpdateLastLoginAsync(user.Id, ct);

                return new AuthResponse(
                    AccessToken: accessToken,
                    RefreshToken: refreshToken,
                    FullName: user.FullName,
                    Email: user.Email,
                    Role: user.Role.ToString(),
                    UserId: user.Id,
                    ExpiresAt: DateTime.UtcNow.AddMinutes(60),
                    TwoFactorRequired: false
                );
            }
            else
            {
                // Wrong recovery code counts as a failed attempt to prevent brute-force
                await HandleFailedAttemptAsync(user, ct);
                throw new UnauthorizedException("Invalid recovery code.");
            }
        }

        // 2. Validate standard OTP
        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            throw new UnauthorizedException("Invalid two-factor configuration.");
        }

        var plainSecret = twoFactor.DecryptSecret(user.TwoFactorSecret);
        if (twoFactor.VerifyOtp(plainSecret, inputCode))
        {
            // Reset failed attempts on success
            await users.UpdateOtpAttemptsAsync(user.Id, 0, null, ct);

            // Generate Auth Tokens
            var accessToken = jwt.GenerateAccessToken(user);
            var refreshToken = jwt.GenerateRefreshToken();
            var expiry = DateTime.UtcNow.AddDays(7);

            await users.UpdateRefreshTokenAsync(user.Id, refreshToken, expiry, ct);
            await users.UpdateLastLoginAsync(user.Id, ct);

            return new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                FullName: user.FullName,
                Email: user.Email,
                Role: user.Role.ToString(),
                UserId: user.Id,
                ExpiresAt: DateTime.UtcNow.AddMinutes(60),
                TwoFactorRequired: false
            );
        }
        else
        {
            await HandleFailedAttemptAsync(user, ct);
            throw new UnauthorizedException("Invalid verification code.");
        }
    }

    private async Task HandleFailedAttemptAsync(User user, CancellationToken ct)
    {
        var newAttempts = user.FailedOtpAttempts + 1;
        DateTime? lockedUntil = null;

        if (newAttempts >= 5)
        {
            lockedUntil = DateTime.UtcNow.AddMinutes(15);
        }

        await users.UpdateOtpAttemptsAsync(user.Id, newAttempts, lockedUntil, ct);
    }
}
