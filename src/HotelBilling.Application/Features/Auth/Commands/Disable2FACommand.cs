using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;

namespace HotelBilling.Application.Features.Auth.Commands;

/// <summary>
/// Command to disable Two-Factor Authentication for the current user.
/// </summary>
public record Disable2FACommand(int UserId, string Password, string Otp) : IRequest<bool>;

/// <summary>
/// Validator for Disable2FACommand.
/// </summary>
public class Disable2FACommandValidator : AbstractValidator<Disable2FACommand>
{
    public Disable2FACommandValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be exactly 6 digits.")
            .Matches("^[0-9]+$").WithMessage("Verification code must contain only numeric digits.");
    }
}

/// <summary>
/// Handler for disabling Two-Factor Authentication.
/// </summary>
public class Disable2FACommandHandler(
    IUserRepository users,
    ITwoFactorService twoFactor) : IRequestHandler<Disable2FACommand, bool>
{
    public async Task<bool> Handle(Disable2FACommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            throw new ConflictException("Two-factor authentication is not enabled on this account.");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid password.");
        }

        // Verify OTP
        var plainSecret = twoFactor.DecryptSecret(user.TwoFactorSecret);
        if (!twoFactor.VerifyOtp(plainSecret, request.Otp))
        {
            throw new UnauthorizedException("Invalid verification code.");
        }

        // Disable 2FA on the user's profile and delete recovery codes
        await users.Disable2FaAsync(user.Id, ct);
        await users.DeleteRecoveryCodesAsync(user.Id, ct);

        return true;
    }
}
