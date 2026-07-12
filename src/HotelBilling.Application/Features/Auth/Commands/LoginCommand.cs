using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
namespace HotelBilling.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public record AuthResponse(string? AccessToken, string? RefreshToken, string FullName, string Email, string Role, int UserId, DateTime? ExpiresAt, bool TwoFactorRequired = false);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandHandler(IUserRepository users, IJwtService jwt) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is inactive.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        // If Two-Factor Authentication is enabled, bypass JWT generation and indicate 2FA is required.
        if (user.TwoFactorEnabled)
        {
            return new AuthResponse(
                AccessToken: null,
                RefreshToken: null,
                FullName: user.FullName,
                Email: user.Email,
                Role: user.Role.ToString(),
                UserId: user.Id,
                ExpiresAt: null,
                TwoFactorRequired: true
            );
        }

        var accessToken  = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        var expiry       = DateTime.UtcNow.AddDays(7);

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
}
