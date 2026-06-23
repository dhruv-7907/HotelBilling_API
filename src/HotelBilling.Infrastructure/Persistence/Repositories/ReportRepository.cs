using Dapper;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class ReportRepository(DapperContext ctx) : IReportRepository
{
    public async Task<IEnumerable<RevenueByChannel>> GetRevenueByChannelAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            SELECT CAST(r.Channel AS NVARCHAR(50)) AS Channel,
                   ISNULL(SUM(i.TotalAmount),0) AS Revenue,
                   COUNT(r.Id) AS Bookings
            FROM Reservations r
            LEFT JOIN Invoices i ON i.ReservationId = r.Id AND i.IsDeleted=0
            WHERE r.IsDeleted=0 AND r.CheckIn BETWEEN @From AND @To
            GROUP BY r.Channel ORDER BY Revenue DESC";
        return await conn.QueryAsync<RevenueByChannel>(sql, new { From = from, To = to });
    }

    public async Task<IEnumerable<RevenueByRoomType>> GetRevenueByRoomTypeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            SELECT CAST(rm.RoomType AS NVARCHAR(50)) AS RoomType,
                   ISNULL(SUM(i.TotalAmount),0) AS Revenue,
                   COUNT(r.Id) AS Bookings
            FROM Reservations r
            JOIN Rooms rm ON r.RoomId = rm.Id
            LEFT JOIN Invoices i ON i.ReservationId = r.Id AND i.IsDeleted=0
            WHERE r.IsDeleted=0 AND r.CheckIn BETWEEN @From AND @To
            GROUP BY rm.RoomType ORDER BY Revenue DESC";
        return await conn.QueryAsync<RevenueByRoomType>(sql, new { From = from, To = to });
    }

    public async Task<HotelKpis> GetKpisAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"
            SELECT
                ISNULL(AVG(r.RatePerNight),0)                                     AS ADR,
                ISNULL(SUM(i.TotalAmount)/NULLIF((SELECT COUNT(*) FROM Rooms WHERE IsDeleted=0),0)/NULLIF(DATEDIFF(DAY,@From,@To),0),0) AS RevPAR,
                ISNULL(AVG(CAST(r.Nights AS FLOAT)),0)                            AS AvgStayNights,
                ISNULL(SUM(CASE WHEN r.Status=6 THEN 1.0 ELSE 0 END)*100.0/NULLIF(COUNT(*),0),0) AS NoShowRate,
                ISNULL(SUM(CASE WHEN r.Status=3 THEN 1.0 ELSE 0 END)*100.0/NULLIF((SELECT COUNT(*) FROM Rooms WHERE IsDeleted=0),0),0) AS OccupancyRate
            FROM Reservations r
            LEFT JOIN Invoices i ON i.ReservationId=r.Id AND i.IsDeleted=0
            WHERE r.IsDeleted=0 AND r.CheckIn BETWEEN @From AND @To";
        return await conn.QueryFirstAsync<HotelKpis>(sql, new { From = from, To = to });
    }
}
