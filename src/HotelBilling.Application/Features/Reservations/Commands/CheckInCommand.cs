using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Reservations.Commands;
public record CheckInCommand(int ReservationId) : IRequest<bool>;
public class CheckInCommandHandler(IReservationRepository repo) : IRequestHandler<CheckInCommand, bool>
{
    public async Task<bool> Handle(CheckInCommand cmd, CancellationToken ct)
    {
        var res = await repo.GetByIdAsync(cmd.ReservationId, ct) ?? throw new NotFoundException("Reservation", cmd.ReservationId);
        if (res.Status != ReservationStatus.Confirmed) throw new ConflictException("Only confirmed reservations can be checked in.");

        var ok = await repo.UpdateReservationAndRoomStatusAsync(
            cmd.ReservationId,
            ReservationStatus.CheckedIn,
            RoomStatus.Occupied,
            ct);

        if (!ok) throw new ConflictException("Unable to complete check-in due to a concurrent update.");
        return ok;
    }
}
