using Dapper;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class GuestRepository(DapperContext ctx) : IGuestRepository
{
    public async Task<PagedResult<Guest>> GetAllAsync(PaginationQuery query, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        var where = "WHERE IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(query.Search))
            where += " AND (FullName LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search)";

        var countSql = $"SELECT COUNT(*) FROM Guests {where}";
        var dataSql  = $@"SELECT * FROM Guests {where}
                          ORDER BY FullName
                          OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var p = new { Search = $"%{query.Search}%", Offset = (query.Page - 1) * query.PageSize, query.PageSize };
        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<Guest>(dataSql, p);
        return new PagedResult<Guest> { Items = items, TotalCount = total, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<Guest?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Guest>("SELECT * FROM Guests WHERE Id=@Id AND IsDeleted=0", new { Id = id });
    }

    public async Task<Guest?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Guest>("SELECT * FROM Guests WHERE Email=@Email AND IsDeleted=0", new { Email = email });
    }

    public async Task<int> CreateAsync(Guest entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            INSERT INTO Guests (FullName, Email, Phone, Address, City, Nationality, IdType, IdNumber, DateOfBirth, IsVip, Notes, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@FullName, @Email, @Phone, @Address, @City, @Nationality, @IdType, @IdNumber, @DateOfBirth, @IsVip, @Notes, @CreatedAt)";
        return await conn.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Guest entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            UPDATE Guests SET FullName=@FullName, Phone=@Phone, Address=@Address, City=@City,
            Nationality=@Nationality, IdType=@IdType, IdNumber=@IdNumber, Notes=@Notes,
            IsVip=@IsVip, UpdatedAt=@UpdatedAt WHERE Id=@Id AND IsDeleted=0";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.ExecuteAsync("UPDATE Guests SET IsDeleted=1, UpdatedAt=@Now WHERE Id=@Id", new { Now = DateTime.UtcNow, Id = id }) > 0;
    }
}
