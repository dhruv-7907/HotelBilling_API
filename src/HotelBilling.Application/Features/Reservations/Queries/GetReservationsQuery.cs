using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Reservations.Queries;

public record GetReservationsQuery(int Page=1, int PageSize=10, string? Search=null, string? Status=null, string? Channel=null) : IRequest<PagedResult<Reservation>>;

public class GetReservationsQueryHandler(IReservationRepository repo) : IRequestHandler<GetReservationsQuery, PagedResult<Reservation>>
{
    public Task<PagedResult<Reservation>> Handle(GetReservationsQuery q, CancellationToken ct)
        => repo.GetAllAsync(new PaginationQuery(q.Page, q.PageSize, q.Search), q.Status, q.Channel, ct);
}
