using MediatR;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Rooms.Queries;
public record GetRoomsQuery(int Page=1, int PageSize=20, string? Status=null, string? Type=null) : IRequest<PagedResult<Room>>;
public class GetRoomsQueryHandler(IRoomRepository repo) : IRequestHandler<GetRoomsQuery, PagedResult<Room>>
{
    public Task<PagedResult<Room>> Handle(GetRoomsQuery q, CancellationToken ct)
        => repo.GetAllAsync(new PaginationQuery(q.Page, q.PageSize), q.Status, q.Type, ct);
}
