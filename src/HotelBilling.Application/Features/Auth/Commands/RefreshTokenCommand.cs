using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler(IUserRepository users, IJwtService jwt) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException("Refresh token is required.");

        var user = await users.GetByRefreshTokenAsync(request.RefreshToken, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (user.RefreshTokenExpiry is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired.");

        var accessToken  = jwt.GenerateAccessToken(user);
        var newRefresh   = jwt.GenerateRefreshToken();
        await users.UpdateRefreshTokenAsync(user.Id, newRefresh, DateTime.UtcNow.AddDays(7), ct);

        return new AuthResponse(accessToken, newRefresh, user.FullName, user.Email, user.Role.ToString(), user.Id, DateTime.UtcNow.AddMinutes(60));
    }
}
