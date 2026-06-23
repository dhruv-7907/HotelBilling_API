using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Guests.Queries;
public record GetGuestByIdQuery(int Id) : IRequest<Guest>;
public class GetGuestByIdQueryHandler(IGuestRepository repo) : IRequestHandler<GetGuestByIdQuery, Guest>
{
    public async Task<Guest> Handle(GetGuestByIdQuery q, CancellationToken ct)
        => await repo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException(nameof(Guest), q.Id);
}
