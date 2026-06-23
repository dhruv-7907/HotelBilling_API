using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Common.Interfaces;
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default);
    Task<int>  CreateAsync(User entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(User entity, CancellationToken ct = default);
    Task<bool> UpdateRefreshTokenAsync(int userId, string? token, DateTime? expiry, CancellationToken ct = default);
    Task<bool> UpdateLastLoginAsync(int userId, CancellationToken ct = default);
}
