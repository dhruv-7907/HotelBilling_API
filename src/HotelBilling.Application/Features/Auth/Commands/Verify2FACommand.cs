using System;
using System.Collections.Generic;
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
/// Response containing generated recovery codes after successfully enabling 2FA.
/// </summary>
public record Verify2FAResponse(IEnumerable<string> RecoveryCodes);

/// <summary>
/// Command to verify the initial OTP and complete Two-Factor Authentication activation.
/// </summary>
public record Verify2FACommand(int UserId, string Otp) : IRequest<Verify2FAResponse>;

/// <summary>
/// Validator for Verify2FACommand.
/// </summary>
public class Verify2FACommandValidator : AbstractValidator<Verify2FACommand>
{
    public Verify2FACommandValidator()
    {
        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be exactly 6 digits.")
            .Matches("^[0-9]+$").WithMessage("Verification code must contain only numeric digits.");
    }
}

/// <summary>
/// Handler for verifying and enabling Two-Factor Authentication.
/// </summary>
public class Verify2FACommandHandler(
    IUserRepository users,
    ITwoFactorService twoFactor) : IRequestHandler<Verify2FACommand, Verify2FAResponse>
{
    public async Task<Verify2FAResponse> Handle(Verify2FACommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            throw new ConflictException("Two-factor authentication setup has not been initiated. Call enable endpoint first.");
        }

        // Decrypt the user's secret key
        var plainSecret = twoFactor.DecryptSecret(user.TwoFactorSecret);

        // Verify OTP
        if (!twoFactor.VerifyOtp(plainSecret, request.Otp))
        {
            throw new UnauthorizedException("Invalid verification code.");
        }

        // Enable 2FA on the user's profile
        await users.Verify2FaAsync(user.Id, ct);

        // Generate 10 recovery codes
        var recoveryCodes = twoFactor.GenerateRecoveryCodes(10).ToList();

        // Hash recovery codes before saving (SHA-256)
        var hashedCodes = recoveryCodes.Select(code =>
        {
            var bytes = Encoding.UTF8.GetBytes(code);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        });

        // Save hashed recovery codes
        await users.SaveRecoveryCodesAsync(user.Id, hashedCodes, ct);

        // Return plain recovery codes once
        return new Verify2FAResponse(recoveryCodes);
    }
}
