namespace HotelBilling.Application.Common.Interfaces;

/// <summary>
/// Service for generating QR Codes and TOTP provisioning URIs for authenticator applications.
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generates a QR Code as a base64-encoded PNG image data URI.
    /// </summary>
    /// <param name="provisioningUri">The otpauth URI configuration.</param>
    /// <returns>A base64 data URL string representing the PNG image.</returns>
    string GenerateQrCode(string provisioningUri);

    /// <summary>
    /// Generates the standard Authenticator provisioning URI (otpauth://).
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="secretKey">The plain-text base32-encoded secret key.</param>
    /// <param name="issuer">The name of the application issuer.</param>
    /// <returns>The formatted provisioning URI.</returns>
    string GenerateProvisioningUri(string email, string secretKey, string issuer = "HotelBilling");
}
