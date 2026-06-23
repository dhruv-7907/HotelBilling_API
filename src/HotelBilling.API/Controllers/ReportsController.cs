using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Features.Reports.Queries;
namespace HotelBilling.API.Controllers;

/// <summary>Analytics reports — revenue by channel, room type, KPIs</summary>
[Authorize(Policy = "AccountsUp")]
public class ReportsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Get full analytics report for a date range.</summary>
    [HttpGet]
    public async Task<IActionResult> GetReport([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => OkResult(await Mediator.Send(new GetReportQuery(from, to), ct));
}
