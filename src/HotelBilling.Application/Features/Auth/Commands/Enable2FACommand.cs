using System.Threading;
using System.Threading.Tasks;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;

namespace HotelBilling.Application.Features.Auth.Commands;

/// <summary>
/// Response returned after initiating 2FA setup containing QR code and manual key.
/// </summary>
public record Enable2FAResponse(string QrCodeUri, string ManualEntryKey);

/// <summary>
/// Command to initiate Two-Factor Authentication setup for the current user.
/// </summary>
public record Enable2FACommand(int UserId) : IRequest<Enable2FAResponse>;

/// <summary>
/// Handler for initiating Two-Factor Authentication setup.
/// </summary>
public class Enable2FACommandHandler(
    IUserRepository users,
    ITwoFactorService twoFactor,
    IQrCodeService qrCode) : IRequestHandler<Enable2FACommand, Enable2FAResponse>
{
    public async Task<Enable2FAResponse> Handle(Enable2FACommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is inactive.");
        }

        // Generate a new TOTP secret key
        var plainSecret = twoFactor.GenerateSecretKey();
        var encryptedSecret = twoFactor.EncryptSecret(plainSecret);

        // Save secret to database, keeping 2FA disabled until verified
        await users.Update2FaSecretAsync(user.Id, encryptedSecret, ct);

        // Generate Authenticator App provisioning URI and QR Code image
        var provisioningUri = qrCode.GenerateProvisioningUri(user.Email, plainSecret, "HotelBillingPro");
        var qrCodeImageBase64 = qrCode.GenerateQrCode(provisioningUri);

        return new Enable2FAResponse(qrCodeImageBase64, plainSecret);
    }
}
