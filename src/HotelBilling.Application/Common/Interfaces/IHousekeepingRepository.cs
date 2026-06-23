using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Common.Interfaces;
public interface IHousekeepingRepository
{
    Task<IEnumerable<HousekeepingTask>> GetAllAsync(CancellationToken ct = default);
    Task<int>  CreateAsync(HousekeepingTask task, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int id, string status, int? userId, CancellationToken ct = default);
    Task<HousekeepingStats> GetStatsAsync(CancellationToken ct = default);
}
public record HousekeepingStats(int Clean, int Dirty, int Inspecting, int DND, int Maintenance);
