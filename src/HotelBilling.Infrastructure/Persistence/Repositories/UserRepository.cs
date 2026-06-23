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
}
