using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;

namespace HotelBilling.Infrastructure.Authentication;

public class JwtService : IJwtService
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly string _audience;
    private readonly double _expiryMinutes;
    private readonly string _issuer;
    private readonly SigningCredentials _signingCredentials;

    public JwtService(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var jwtKey = GetRequiredSetting(config, "Jwt:Key");

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
        }

        _issuer = GetRequiredSetting(config, "Jwt:Issuer");
        _audience = GetRequiredSetting(config, "Jwt:Audience");
        _expiryMinutes = GetPositiveDouble(config["Jwt:ExpiryMinutes"], "Jwt:ExpiryMinutes", 60);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string GenerateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: _signingCredentials);

        return _handler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string GetRequiredSetting(IConfiguration config, string key)
    {
        var value = config[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required.");
        }

        return value;
    }

    private static double GetPositiveDouble(string? value, string key, double defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!double.TryParse(value, out var parsedValue) || parsedValue <= 0)
        {
            throw new InvalidOperationException($"{key} must be a positive number.");
        }

        return parsedValue;
    }
}
