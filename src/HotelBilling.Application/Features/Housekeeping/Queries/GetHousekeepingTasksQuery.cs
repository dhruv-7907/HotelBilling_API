using MediatR;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Housekeeping.Queries;
public record GetHousekeepingTasksQuery : IRequest<IEnumerable<HousekeepingTask>>;
public class GetHousekeepingTasksQueryHandler(IHousekeepingRepository repo) : IRequestHandler<GetHousekeepingTasksQuery, IEnumerable<HousekeepingTask>>
{
    public Task<IEnumerable<HousekeepingTask>> Handle(GetHousekeepingTasksQuery _, CancellationToken ct) => repo.GetAllAsync(ct);
}
