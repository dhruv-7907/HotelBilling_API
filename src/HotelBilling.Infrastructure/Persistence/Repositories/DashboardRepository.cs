using Dapper;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class DashboardRepository(DapperContext ctx) : IDashboardRepository
{
    public async Task<DashboardStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        var today     = DateTime.Today;
        var yesterday = today.AddDays(-1);

        var todayRev = await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(TotalAmount),0) FROM Invoices WHERE CAST(InvoiceDate AS DATE)=@D AND IsDeleted=0", new { D = today });
        var yestRev = await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(TotalAmount),0) FROM Invoices WHERE CAST(InvoiceDate AS DATE)=@D AND IsDeleted=0", new { D = yesterday });
        var occupiedRooms = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Rooms WHERE Status=2 AND IsDeleted=0");   // 2 = Occupied
        var totalRooms = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Rooms WHERE IsDeleted=0");
        var activeGuests = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Reservations WHERE Status=3 AND IsDeleted=0"); // 3 = CheckedIn
        var checkoutsToday = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Reservations WHERE Status=4 AND CAST(UpdatedAt AS DATE)=@D AND IsDeleted=0", new { D = today });
        var pendingAmt = await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(BalanceDue),0) FROM Invoices WHERE Status IN (2,4) AND IsDeleted=0"); // Pending=2, Overdue=4
        var overdueCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Invoices WHERE Status=4 AND IsDeleted=0");

        const string revMonthSql = @"
            SELECT FORMAT(InvoiceDate,'MMM') AS Month,
                   ISNULL(SUM(CASE WHEN li.Description LIKE '%Room%' THEN li.Amount ELSE 0 END),0) AS Rooms,
                   ISNULL(SUM(CASE WHEN li.Description LIKE '%F&B%' OR li.Description LIKE '%Restaurant%' THEN li.Amount ELSE 0 END),0) AS FB,
                   ISNULL(SUM(CASE WHEN li.Description LIKE '%Spa%' THEN li.Amount ELSE 0 END),0) AS Spa,
                   ISNULL(SUM(CASE WHEN li.Description NOT LIKE '%Room%' AND li.Description NOT LIKE '%F&B%'
                                        AND li.Description NOT LIKE '%Restaurant%' AND li.Description NOT LIKE '%Spa%'
                               THEN li.Amount ELSE 0 END),0) AS Other
            FROM Invoices i
            JOIN InvoiceLineItems li ON i.Id = li.InvoiceId
            WHERE i.IsDeleted=0 AND i.InvoiceDate >= DATEADD(MONTH,-6,GETDATE())
            GROUP BY FORMAT(InvoiceDate,'MMM'), MONTH(InvoiceDate)
            ORDER BY MONTH(InvoiceDate)";

        const string occDaySql = @"
            SELECT DATENAME(WEEKDAY, CheckIn) AS Day,
                   CAST(COUNT(*)*100.0/NULLIF((SELECT COUNT(*) FROM Rooms WHERE IsDeleted=0),0) AS INT) AS Rate
            FROM Reservations
            WHERE Status IN (2,3) AND CheckIn >= DATEADD(DAY,-7,GETDATE()) AND IsDeleted=0
            GROUP BY DATENAME(WEEKDAY,CheckIn), DATEPART(WEEKDAY,CheckIn)
            ORDER BY DATEPART(WEEKDAY,CheckIn)";

        var revenueByMonth = await conn.QueryAsync<RevenueByMonth>(revMonthSql);
        var occupancyByDay = await conn.QueryAsync<OccupancyByDay>(occDaySql);

        return new DashboardStats(todayRev, yestRev, occupiedRooms, totalRooms,
            activeGuests, checkoutsToday, pendingAmt, overdueCount,
            revenueByMonth, occupancyByDay);
    }
}
