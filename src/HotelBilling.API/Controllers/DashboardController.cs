using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Features.Dashboard.Queries;
namespace HotelBilling.API.Controllers;

/// <summary>Dashboard KPIs and summary statistics</summary>
[Authorize(Policy = "FrontDeskUp")]
public class DashboardController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Get today's KPIs: revenue, occupancy, active guests, pending invoices, charts.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => OkResult(await Mediator.Send(new GetDashboardStatsQuery(), ct));
}
