using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using OtpNet;
using HotelBilling.Application.Common.Interfaces;

namespace HotelBilling.Infrastructure.Services;

/// <summary>
/// Service implementing Two-Factor Authentication capabilities using Otp.NET and AES-256 encryption.
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly IConfiguration _config;

    public TwoFactorService(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public string GenerateSecretKey()
    {
        var bytes = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(bytes);
    }

    /// <inheritdoc />
    public bool VerifyOtp(string secretKey, string otp)
    {
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(otp))
        {
            return false;
        }

        try
        {
            var bytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(bytes);
            
            // Allow 30 seconds drift tolerance (previous step and next step)
            var result = totp.VerifyTotp(
                otp, 
                out _, 
                new VerificationWindow(previous: 1, future: 1));
                
            return result;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        for (int c = 0; c < count; c++)
        {
            var result = new char[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
            // Format as XXXX-XXXX for better user readability
            codes.Add($"{new string(result[..4])}-{new string(result[4..])}");
        }
        
        return codes;
    }

    /// <inheritdoc />
    public string EncryptSecret(string plainSecret)
    {
        if (string.IsNullOrEmpty(plainSecret))
        {
            return plainSecret;
        }

        var key = GetEncryptionKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainSecret);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string DecryptSecret(string encryptedSecret)
    {
        if (string.IsNullOrEmpty(encryptedSecret))
        {
            return encryptedSecret;
        }

        var key = GetEncryptionKey();
        var fullCipher = Convert.FromBase64String(encryptedSecret);

        using var aes = Aes.Create();
        aes.Key = key;

        var ivLength = aes.BlockSize / 8;
        if (fullCipher.Length < ivLength)
        {
            throw new CryptographicException("Invalid ciphertext length.");
        }

        var iv = new byte[ivLength];
        var cipherBytes = new byte[fullCipher.Length - ivLength];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, ivLength);
        Buffer.BlockCopy(fullCipher, ivLength, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] GetEncryptionKey()
    {
        var keyStr = _config["TwoFactor:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(keyStr))
        {
            throw new InvalidOperationException("TwoFactor:EncryptionKey configuration is missing in appsettings.json.");
        }

        try
        {
            // Attempt to load as Base64 encoded key
            return Convert.FromBase64String(keyStr);
        }
        catch (FormatException)
        {
            // Fallback to UTF-8 raw string representation, padded/truncated to 32 bytes (256 bits)
            var bytes = Encoding.UTF8.GetBytes(keyStr);
            if (bytes.Length >= 32)
            {
                return bytes[..32];
            }
            
            var padded = new byte[32];
            Array.Copy(bytes, padded, bytes.Length);
            return padded;
        }
    }
}
