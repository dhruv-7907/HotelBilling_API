using MediatR;
using HotelBilling.Application.Common.Interfaces;
namespace HotelBilling.Application.Features.Reports.Queries;
public record GetReportQuery(DateTime From, DateTime To) : IRequest<ReportResult>;
public record ReportResult(IEnumerable<RevenueByChannel> ByChannel, IEnumerable<RevenueByRoomType> ByRoomType, HotelKpis Kpis);
public class GetReportQueryHandler(IReportRepository repo) : IRequestHandler<GetReportQuery, ReportResult>
{
    public async Task<ReportResult> Handle(GetReportQuery q, CancellationToken ct)
    {
        var channel  = await repo.GetRevenueByChannelAsync(q.From, q.To, ct);
        var roomType = await repo.GetRevenueByRoomTypeAsync(q.From, q.To, ct);
        var kpis     = await repo.GetKpisAsync(q.From, q.To, ct);
        return new ReportResult(channel, roomType, kpis);
    }
}
