using System;
using QRCoder;
using HotelBilling.Application.Common.Interfaces;

namespace HotelBilling.Infrastructure.Services;

/// <summary>
/// Service implementing QR Code generation using QRCoder.
/// </summary>
public class QrCodeService : IQrCodeService
{
    /// <inheritdoc />
    public string GenerateQrCode(string provisioningUri)
    {
        if (string.IsNullOrWhiteSpace(provisioningUri))
        {
            throw new ArgumentException("Provisioning URI cannot be null or empty.", nameof(provisioningUri));
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
    }

    /// <inheritdoc />
    public string GenerateProvisioningUri(string email, string secretKey, string issuer = "HotelBilling")
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        }
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));
        }

        var escapedIssuer = Uri.EscapeDataString(issuer);
        var escapedEmail = Uri.EscapeDataString(email);
        
        return $"otpauth://totp/{escapedIssuer}:{escapedEmail}?secret={secretKey}&issuer={escapedIssuer}&algorithm=SHA1&digits=6&period=30";
    }
}
