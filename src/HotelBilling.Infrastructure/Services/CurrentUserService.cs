using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public int?    UserId        => int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
    public string? Email         => User?.FindFirstValue(JwtRegisteredClaimNames.Email);
    public string? Role          => User?.FindFirstValue(ClaimTypes.Role);
    public bool    IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
