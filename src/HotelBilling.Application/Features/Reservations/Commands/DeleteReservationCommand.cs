using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Application.Features.Reservations.Commands;
public record DeleteReservationCommand(int Id) : IRequest<bool>;
public class DeleteReservationCommandHandler(IReservationRepository repo) : IRequestHandler<DeleteReservationCommand, bool>
{
    public async Task<bool> Handle(DeleteReservationCommand cmd, CancellationToken ct)
    {
        var exists = await repo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException("Reservation", cmd.Id);
        return await repo.DeleteAsync(cmd.Id, ct);
    }
}
