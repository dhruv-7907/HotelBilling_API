using System.Collections.Generic;

namespace HotelBilling.Application.Common.Interfaces;

/// <summary>
/// Service for managing Time-based One-Time Passwords (TOTP) and 2FA recovery codes.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a new secure, random base32-encoded 2FA secret key.
    /// </summary>
    /// <returns>A base32-encoded secret key.</returns>
    string GenerateSecretKey();

    /// <summary>
    /// Verifies a time-based one-time password (TOTP) against a base32-encoded secret key.
    /// </summary>
    /// <param name="secretKey">The plain-text base32-encoded secret key.</param>
    /// <param name="otp">The 6-digit OTP code entered by the user.</param>
    /// <returns>True if the OTP code is valid within the time step (with drift tolerance), otherwise false.</returns>
    bool VerifyOtp(string secretKey, string otp);

    /// <summary>
    /// Generates a set of cryptographically secure random recovery codes.
    /// </summary>
    /// <param name="count">The number of recovery codes to generate.</param>
    /// <returns>A list of plain-text recovery codes.</returns>
    IEnumerable<string> GenerateRecoveryCodes(int count = 10);

    /// <summary>
    /// Encrypts the user's plain-text 2FA secret key using a symmetric key.
    /// </summary>
    /// <param name="plainSecret">The plain-text secret key.</param>
    /// <returns>The base64-encoded encrypted secret key string.</returns>
    string EncryptSecret(string plainSecret);

    /// <summary>
    /// Decrypts the user's encrypted 2FA secret key using a symmetric key.
    /// </summary>
    /// <param name="encryptedSecret">The base64-encoded encrypted secret key string.</param>
    /// <returns>The decrypted plain-text secret key.</returns>
    string DecryptSecret(string encryptedSecret);
}
