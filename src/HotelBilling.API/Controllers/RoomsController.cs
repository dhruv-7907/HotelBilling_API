using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Features.Rooms.Commands;
using HotelBilling.Application.Features.Rooms.Queries;
namespace HotelBilling.API.Controllers;

/// <summary>Room inventory and status management</summary>
[Authorize(Policy = "FrontDeskUp")]
public class RoomsController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page=1, [FromQuery] int pageSize=20,
        [FromQuery] string? status=null, [FromQuery] string? type=null, CancellationToken ct=default)
        => OkResult(await Mediator.Send(new GetRoomsQuery(page, pageSize, status, type), ct));

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateRoomCommand command, CancellationToken ct)
        => CreatedResult(await Mediator.Send(command, ct), "Room created");

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRoomStatusRequest req, CancellationToken ct)
        => OkResult(await Mediator.Send(new UpdateRoomStatusCommand(id, req.Status), ct), "Room status updated");
}

public record UpdateRoomStatusRequest(string Status);
