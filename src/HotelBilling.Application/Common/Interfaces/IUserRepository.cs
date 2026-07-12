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

    // 2FA Methods
    Task<bool> Update2FaSecretAsync(int userId, string? encryptedSecret, CancellationToken ct = default);
    Task<bool> Verify2FaAsync(int userId, CancellationToken ct = default);
    Task<bool> Disable2FaAsync(int userId, CancellationToken ct = default);
    Task<bool> UpdateOtpAttemptsAsync(int userId, int failedAttempts, DateTime? lockedUntil, CancellationToken ct = default);
    Task<bool> SaveRecoveryCodesAsync(int userId, IEnumerable<string> codeHashes, CancellationToken ct = default);
    Task<IEnumerable<RecoveryCode>> GetRecoveryCodesAsync(int userId, CancellationToken ct = default);
    Task<bool> MarkRecoveryCodeAsUsedAsync(int userId, string codeHash, CancellationToken ct = default);
    Task<bool> DeleteRecoveryCodesAsync(int userId, CancellationToken ct = default);
}
