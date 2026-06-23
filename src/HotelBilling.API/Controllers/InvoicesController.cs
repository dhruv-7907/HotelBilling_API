using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBilling.Application.Features.Invoices.Commands;
using HotelBilling.Application.Features.Invoices.Queries;
namespace HotelBilling.API.Controllers;

/// <summary>Invoice management — create, view, update status</summary>
[Authorize(Policy = "AccountsUp")]
public class InvoicesController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Get paginated invoices with optional status filter.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page=1, [FromQuery] int pageSize=10,
        [FromQuery] string? search=null, [FromQuery] string? status=null, CancellationToken ct=default)
        => OkResult(await Mediator.Send(new GetInvoicesQuery(page, pageSize, search, status), ct));

    /// <summary>Get invoice with all line items by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => OkResult(await Mediator.Send(new GetInvoiceByIdQuery(id), ct));

    /// <summary>Create a new invoice with line items. Calculates GST automatically.</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceCommand command, CancellationToken ct)
        => CreatedResult(await Mediator.Send(command, ct), "Invoice created successfully");

    /// <summary>Update invoice status (Pending, Paid, Overdue, Cancelled).</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
        => OkResult(await Mediator.Send(new UpdateInvoiceStatusCommand(id, req.Status), ct), "Invoice status updated");

    /// <summary>Delete (soft) an invoice.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => OkResult(await Mediator.Send(new DeleteInvoiceCommand(id), ct), "Invoice deleted");
}

public record UpdateStatusRequest(string Status);
