using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Features.Housekeeping.Commands;
using HotelBilling.Application.Features.Housekeeping.Queries;
namespace HotelBilling.API.Controllers;

/// <summary>Housekeeping tasks and room cleaning management</summary>
[Authorize(Policy = "HousekeepingUp")]
public class HousekeepingController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(CancellationToken ct)
        => OkResult(await Mediator.Send(new GetHousekeepingTasksQuery(), ct));

    [HttpPost("tasks")]
    public async Task<IActionResult> AssignTask([FromBody] AssignHousekeepingTaskCommand command, CancellationToken ct)
        => CreatedResult(await Mediator.Send(command, ct), "Task assigned");

    [HttpPatch("tasks/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusRequest req, CancellationToken ct)
        => OkResult(await Mediator.Send(new UpdateTaskStatusCommand(id, req.Status, req.UserId), ct), "Task updated");
}

public record UpdateTaskStatusRequest(string Status, int? UserId);
