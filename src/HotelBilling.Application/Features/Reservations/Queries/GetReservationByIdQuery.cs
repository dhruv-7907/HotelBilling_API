using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Reservations.Queries;
public record GetReservationByIdQuery(int Id) : IRequest<Reservation>;
public class GetReservationByIdQueryHandler(IReservationRepository repo) : IRequestHandler<GetReservationByIdQuery, Reservation>
{
    public async Task<Reservation> Handle(GetReservationByIdQuery q, CancellationToken ct)
        => await repo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException(nameof(Reservation), q.Id);
}
