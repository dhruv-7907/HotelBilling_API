namespace HotelBilling.Application.Common.Interfaces;
public interface IDashboardRepository
{
    Task<DashboardStats> GetStatsAsync(CancellationToken ct = default);
}
public record DashboardStats(
    decimal TodayRevenue, decimal YesterdayRevenue,
    int OccupiedRooms, int TotalRooms,
    int ActiveGuests, int TotalCheckoutsToday,
    decimal PendingInvoicesAmount, int OverdueInvoiceCount,
    IEnumerable<RevenueByMonth> RevenueByMonth,
    IEnumerable<OccupancyByDay> OccupancyByDay
);
public record RevenueByMonth(string Month, decimal Rooms, decimal FB, decimal Spa, decimal Other);
public record OccupancyByDay(string Day, int Rate);
