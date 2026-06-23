using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Reservations.Commands;
public record CheckOutCommand(int ReservationId) : IRequest<bool>;
public class CheckOutCommandHandler(IReservationRepository repo) : IRequestHandler<CheckOutCommand, bool>
{
    public async Task<bool> Handle(CheckOutCommand cmd, CancellationToken ct)
    {
        var res = await repo.GetByIdAsync(cmd.ReservationId, ct) ?? throw new NotFoundException("Reservation", cmd.ReservationId);
        if (res.Status != ReservationStatus.CheckedIn) throw new ConflictException("Only checked-in reservations can be checked out.");

        var ok = await repo.UpdateReservationAndRoomStatusAsync(
            cmd.ReservationId,
            ReservationStatus.CheckedOut,
            RoomStatus.Dirty,
            ct);

        if (!ok) throw new ConflictException("Unable to complete check-out due to a concurrent update.");
        return ok;
    }
}
