using MediatR;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Application.Features.Dashboard.Queries;
public record GetDashboardStatsQuery : IRequest<DashboardStats>;
public class GetDashboardStatsQueryHandler(IDashboardRepository repo) : IRequestHandler<GetDashboardStatsQuery, DashboardStats>
{
    public Task<DashboardStats> Handle(GetDashboardStatsQuery _, CancellationToken ct)
        => repo.GetStatsAsync(ct);
}
