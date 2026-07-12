using Dapper;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class UserRepository(DapperContext ctx) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "SELECT * FROM Users WHERE Id = @Id AND IsDeleted = 0";
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "SELECT * FROM Users WHERE Email = @Email AND IsDeleted = 0";
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Email = email.ToLowerInvariant() });
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "SELECT * FROM Users WHERE RefreshToken = @RefreshToken AND IsDeleted = 0";
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { RefreshToken = refreshToken });
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "SELECT * FROM Users WHERE IsDeleted = 0 ORDER BY FullName";
        return await conn.QueryAsync<User>(sql);
    }

    public async Task<int> CreateAsync(User entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, IsActive, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@FullName, @Email, @Phone, @PasswordHash, @Role, @IsActive, @CreatedAt)";
        return await conn.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(User entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            UPDATE Users SET FullName=@FullName, Phone=@Phone, Role=@Role,
            IsActive=@IsActive, UpdatedAt=@UpdatedAt WHERE Id=@Id AND IsDeleted=0";
        entity.UpdatedAt = DateTime.UtcNow;
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> UpdateRefreshTokenAsync(int userId, string? token, DateTime? expiry, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "UPDATE Users SET RefreshToken=@Token, RefreshTokenExpiry=@Expiry WHERE Id=@UserId";
        return await conn.ExecuteAsync(sql, new { Token = token, Expiry = expiry, UserId = userId }) > 0;
    }

    public async Task<bool> UpdateLastLoginAsync(int userId, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "UPDATE Users SET LastLoginAt=@Now WHERE Id=@UserId";
        return await conn.ExecuteAsync(sql, new { Now = DateTime.UtcNow, UserId = userId }) > 0;
    }

    public async Task<bool> Update2FaSecretAsync(int userId, string? encryptedSecret, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "UPDATE Users SET TwoFactorSecret = @Secret, TwoFactorEnabled = 0 WHERE Id = @UserId";
        return await conn.ExecuteAsync(sql, new { Secret = encryptedSecret, UserId = userId }) > 0;
    }

    public async Task<bool> Verify2FaAsync(int userId, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "UPDATE Users SET TwoFactorEnabled = 1, TwoFactorVerifiedAt = @Now WHERE Id = @UserId";
        return await conn.ExecuteAsync(sql, new { Now = DateTime.UtcNow, UserId = userId }) > 0;
    }

    public async Task<bool> Disable2FaAsync(int userId, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "UPDATE Users SET TwoFactorEnabled = 0, TwoFactorSecret = NULL, TwoFactorVerifiedAt = NULL, FailedOtpAttempts = 0, OtpLockedUntil = NULL WHERE Id = @UserId";
        return await conn.ExecuteAsync(sql, new { UserId = userId }) > 0;
    }

    public async Task<bool> UpdateOtpAttemptsAsync(int userId, int failedAttempts, DateTime? lockedUntil, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "UPDATE Users SET FailedOtpAttempts = @FailedAttempts, OtpLockedUntil = @LockedUntil WHERE Id = @UserId";
        return await conn.ExecuteAsync(sql, new { FailedAttempts = failedAttempts, LockedUntil = lockedUntil, UserId = userId }) > 0;
    }

    public async Task<bool> SaveRecoveryCodesAsync(int userId, IEnumerable<string> codeHashes, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            // Delete existing recovery codes
            const string deleteSql = "DELETE FROM RecoveryCodes WHERE UserId = @UserId";
            await conn.ExecuteAsync(deleteSql, new { UserId = userId }, transaction);

            // Insert new recovery codes
            const string insertSql = @"
                INSERT INTO RecoveryCodes (UserId, CodeHash, Used, UsedAt, CreatedAt)
                VALUES (@UserId, @CodeHash, 0, NULL, @Now)";
            
            var now = DateTime.UtcNow;
            foreach (var hash in codeHashes)
            {
                await conn.ExecuteAsync(insertSql, new { UserId = userId, CodeHash = hash, Now = now }, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<RecoveryCode>> GetRecoveryCodesAsync(int userId, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "SELECT * FROM RecoveryCodes WHERE UserId = @UserId";
        return await conn.QueryAsync<RecoveryCode>(sql, new { UserId = userId });
    }

    public async Task<bool> MarkRecoveryCodeAsUsedAsync(int userId, string codeHash, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "UPDATE RecoveryCodes SET Used = 1, UsedAt = @Now WHERE UserId = @UserId AND CodeHash = @CodeHash AND Used = 0";
        return await conn.ExecuteAsync(sql, new { Now = DateTime.UtcNow, UserId = userId, CodeHash = codeHash }) > 0;
    }

    public async Task<bool> DeleteRecoveryCodesAsync(int userId, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = "DELETE FROM RecoveryCodes WHERE UserId = @UserId";
        return await conn.ExecuteAsync(sql, new { UserId = userId }) > 0;
    }
}
