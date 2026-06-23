using MediatR;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Guests.Queries;
public record GetGuestsQuery(int Page=1, int PageSize=10, string? Search=null) : IRequest<PagedResult<Guest>>;
public class GetGuestsQueryHandler(IGuestRepository repo) : IRequestHandler<GetGuestsQuery, PagedResult<Guest>>
{
    public Task<PagedResult<Guest>> Handle(GetGuestsQuery q, CancellationToken ct)
        => repo.GetAllAsync(new PaginationQuery(q.Page, q.PageSize, q.Search), ct);
}
