using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Common.Interfaces;
public interface IReservationRepository
{
    Task<PagedResult<Reservation>> GetAllAsync(PaginationQuery query, string? status, string? channel, CancellationToken ct = default);
    Task<Reservation?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Reservation?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<int>  CreateAsync(Reservation entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(Reservation entity, CancellationToken ct = default);
    Task<bool> UpdateReservationAndRoomStatusAsync(int reservationId, ReservationStatus reservationStatus, RoomStatus roomStatus, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> RoomIsAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId = null, CancellationToken ct = default);
}
