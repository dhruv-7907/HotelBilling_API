namespace HotelBilling.Application.Common.Interfaces;
public interface IReportRepository
{
    Task<IEnumerable<RevenueByChannel>> GetRevenueByChannelAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<RevenueByRoomType>> GetRevenueByRoomTypeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<HotelKpis> GetKpisAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
public record RevenueByChannel(string Channel, decimal Revenue, int Bookings);
public record RevenueByRoomType(string RoomType, decimal Revenue, int Bookings);
public record HotelKpis(decimal ADR, decimal RevPAR, decimal AvgStayNights, decimal NoShowRate, decimal OccupancyRate);
