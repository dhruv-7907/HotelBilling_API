using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;

namespace HotelBilling.Application.Features.Auth.Commands;

/// <summary>
/// Command to regenerate Two-Factor Authentication recovery codes for the current user.
/// </summary>
public record GenerateRecoveryCodesCommand(int UserId) : IRequest<IEnumerable<string>>;

/// <summary>
/// Handler for regenerating recovery codes.
/// </summary>
public class GenerateRecoveryCodesCommandHandler(
    IUserRepository users,
    ITwoFactorService twoFactor) : IRequestHandler<GenerateRecoveryCodesCommand, IEnumerable<string>>
{
    public async Task<IEnumerable<string>> Handle(GenerateRecoveryCodesCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            throw new ConflictException("Two-factor authentication is not enabled on this account.");
        }

        // Generate 10 new recovery codes
        var recoveryCodes = twoFactor.GenerateRecoveryCodes(10).ToList();

        // Hash recovery codes (SHA-256)
        var hashedCodes = recoveryCodes.Select(code =>
        {
            var bytes = Encoding.UTF8.GetBytes(code);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        });

        // Save hashed recovery codes (deletes old ones inside transaction)
        await users.SaveRecoveryCodesAsync(user.Id, hashedCodes, ct);

        // Return plain recovery codes once
        return recoveryCodes;
    }
}
