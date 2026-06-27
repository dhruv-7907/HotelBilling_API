using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using HotelBilling.Application.Common.Interfaces;

namespace HotelBilling.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return int.TryParse(userId, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email);
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
