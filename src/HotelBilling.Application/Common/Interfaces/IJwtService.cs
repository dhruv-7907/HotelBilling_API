using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Common.Interfaces;
public interface IJwtService
{
    string  GenerateAccessToken(User user);
    string  GenerateRefreshToken();
}
