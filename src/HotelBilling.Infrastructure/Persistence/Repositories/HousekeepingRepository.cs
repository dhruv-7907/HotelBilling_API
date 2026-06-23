using Dapper;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class HousekeepingRepository(DapperContext ctx) : IHousekeepingRepository
{
    public async Task<IEnumerable<HousekeepingTask>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            SELECT ht.*, r.RoomNumber, r.RoomType, u.FullName AS AssignedToName
            FROM HousekeepingTasks ht
            JOIN Rooms r ON ht.RoomId = r.Id
            LEFT JOIN Users u ON ht.AssignedToId = u.Id
            WHERE ht.IsDeleted=0 ORDER BY ht.CreatedAt DESC";
        return await conn.QueryAsync<HousekeepingTask>(sql);
    }

    public async Task<int> CreateAsync(HousekeepingTask task, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            INSERT INTO HousekeepingTasks (RoomId,AssignedToId,TaskType,Status,Notes,CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@RoomId,@AssignedToId,@TaskType,@Status,@Notes,@CreatedAt)";
        return await conn.ExecuteScalarAsync<int>(sql, task);
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, int? userId, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        var completedAt = status == "Completed" ? (DateTime?)DateTime.UtcNow : null;
        return await conn.ExecuteAsync(
            "UPDATE HousekeepingTasks SET Status=@Status, CompletedAt=@CompletedAt, UpdatedAt=@Now WHERE Id=@Id",
            new { Status=status, CompletedAt=completedAt, Now=DateTime.UtcNow, Id=id }) > 0;
    }

    public async Task<HousekeepingStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            SELECT
                SUM(CASE WHEN Status=4 THEN 1 ELSE 0 END) AS Clean,
                SUM(CASE WHEN Status=3 THEN 1 ELSE 0 END) AS Dirty,
                SUM(CASE WHEN Status=6 THEN 1 ELSE 0 END) AS Inspecting,
                SUM(CASE WHEN Status=7 THEN 1 ELSE 0 END) AS DND,
                SUM(CASE WHEN Status=5 THEN 1 ELSE 0 END) AS Maintenance
            FROM Rooms WHERE IsDeleted=0";
        return await conn.QueryFirstAsync<HousekeepingStats>(sql);
    }
}
