using Dapper;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class RoomRepository(DapperContext ctx) : IRoomRepository
{
    public async Task<PagedResult<Room>> GetAllAsync(PaginationQuery query, string? status, string? type, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        var conditions = new List<string> { "IsDeleted = 0" };
        var statusId = ParseEnumOrNumeric<RoomStatus>(status);
        var typeId   = ParseEnumOrNumeric<RoomType>(type);

        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add(statusId.HasValue ? "Status = @StatusId" : "1 = 0");

        if (!string.IsNullOrWhiteSpace(type))
            conditions.Add(typeId.HasValue ? "RoomType = @TypeId" : "1 = 0");

        if (!string.IsNullOrEmpty(query.Search)) conditions.Add("RoomNumber LIKE @Search");

        var where   = "WHERE " + string.Join(" AND ", conditions);
        var total   = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Rooms {where}", new { StatusId = statusId, TypeId = typeId, Search = $"%{query.Search}%" });
        var items   = await conn.QueryAsync<Room>($"SELECT * FROM Rooms {where} ORDER BY Floor, RoomNumber OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { StatusId = statusId, TypeId = typeId, Search = $"%{query.Search}%", Offset = (query.Page-1)*query.PageSize, query.PageSize });
        return new PagedResult<Room> { Items = items, TotalCount = total, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<Room?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Room>("SELECT * FROM Rooms WHERE Id=@Id AND IsDeleted=0", new { Id = id });
    }

    public async Task<Room?> GetByNumberAsync(string number, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Room>("SELECT * FROM Rooms WHERE RoomNumber=@Number AND IsDeleted=0", new { Number = number });
    }

    public async Task<int> CreateAsync(Room entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            INSERT INTO Rooms (RoomNumber, RoomType, Floor, Status, RatePerNight, MaxOccupancy, HasMinibar, HasJacuzzi, Description, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@RoomNumber, @RoomType, @Floor, @Status, @RatePerNight, @MaxOccupancy, @HasMinibar, @HasJacuzzi, @Description, @CreatedAt)";
        return await conn.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Room entity, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"UPDATE Rooms SET RoomType=@RoomType, Floor=@Floor, RatePerNight=@RatePerNight,
            MaxOccupancy=@MaxOccupancy, HasMinibar=@HasMinibar, HasJacuzzi=@HasJacuzzi,
            Description=@Description, UpdatedAt=@UpdatedAt WHERE Id=@Id AND IsDeleted=0";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> UpdateStatusAsync(int id, RoomStatus status, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.ExecuteAsync("UPDATE Rooms SET Status=@Status, UpdatedAt=@Now WHERE Id=@Id",
            new { Status = (int)status, Now = DateTime.UtcNow, Id = id }) > 0;
    }

    private static int? ParseEnumOrNumeric<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(TEnum), numeric)) return numeric;
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? Convert.ToInt32(parsed) : null;
    }
}
