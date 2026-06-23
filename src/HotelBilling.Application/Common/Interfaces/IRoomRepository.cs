using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Common.Interfaces;
public interface IRoomRepository
{
    Task<PagedResult<Room>> GetAllAsync(PaginationQuery query, string? status, string? type, CancellationToken ct = default);
    Task<Room?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Room?> GetByNumberAsync(string number, CancellationToken ct = default);
    Task<int>  CreateAsync(Room entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(Room entity, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int id, RoomStatus status, CancellationToken ct = default);
}
