using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Features.Reservations.Commands;
using HotelBilling.Application.Features.Reservations.Queries;
namespace HotelBilling.API.Controllers;

/// <summary>Manage hotel reservations — CRUD, check-in, check-out</summary>
[Authorize(Policy = "FrontDeskUp")]
public class ReservationsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Get paginated list of reservations with optional filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] string? status = null,
        [FromQuery] string? channel = null, CancellationToken ct = default)
        => OkResult(await Mediator.Send(new GetReservationsQuery(page, pageSize, search, status, channel), ct));

    /// <summary>Get a single reservation by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => OkResult(await Mediator.Send(new GetReservationByIdQuery(id), ct));

    /// <summary>Create a new reservation. Validates room availability.</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromBody] CreateReservationCommand command, CancellationToken ct)
        => CreatedResult(await Mediator.Send(command, ct), "Reservation created successfully");

    /// <summary>Update reservation details.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservationCommand command, CancellationToken ct)
        => OkResult(await Mediator.Send(command with { Id = id }, ct), "Reservation updated");

    /// <summary>Soft-delete a reservation.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => OkResult(await Mediator.Send(new DeleteReservationCommand(id), ct), "Reservation deleted");

    /// <summary>Check-in a confirmed reservation. Sets room to Occupied.</summary>
    [HttpPost("{id:int}/check-in")]
    public async Task<IActionResult> CheckIn(int id, CancellationToken ct)
        => OkResult(await Mediator.Send(new CheckInCommand(id), ct), "Guest checked in successfully");

    /// <summary>Check-out a checked-in reservation. Sets room to Dirty.</summary>
    [HttpPost("{id:int}/check-out")]
    public async Task<IActionResult> CheckOut(int id, CancellationToken ct)
        => OkResult(await Mediator.Send(new CheckOutCommand(id), ct), "Guest checked out successfully");
}
