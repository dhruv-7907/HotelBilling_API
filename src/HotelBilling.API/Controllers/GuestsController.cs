using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Features.Guests.Commands;
using HotelBilling.Application.Features.Guests.Queries;
namespace HotelBilling.API.Controllers;

/// <summary>Guest profile management</summary>
[Authorize(Policy = "FrontDeskUp")]
public class GuestsController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page=1, [FromQuery] int pageSize=10, [FromQuery] string? search=null, CancellationToken ct=default)
        => OkResult(await Mediator.Send(new GetGuestsQuery(page, pageSize, search), ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => OkResult(await Mediator.Send(new GetGuestByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuestCommand command, CancellationToken ct)
        => CreatedResult(await Mediator.Send(command, ct), "Guest created");

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGuestCommand command, CancellationToken ct)
        => OkResult(await Mediator.Send(command with { Id = id }, ct), "Guest updated");
}
