using FluentValidation;
using MediatR;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Features.Invoices.Commands;

public record InvoiceLineItemDto(string Description, int Quantity, decimal UnitPrice, decimal GstRate = 18);

public record CreateInvoiceCommand(
    int ReservationId, int GuestId, DateTime DueDate,
    IEnumerable<InvoiceLineItemDto> LineItems,
    decimal DiscountAmount = 0,
    PaymentMethod? PaymentMethod = null,
    string? Notes = null
) : IRequest<int>;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.GuestId).GreaterThan(0);
        RuleFor(x => x.DueDate).GreaterThan(DateTime.Today);
        RuleFor(x => x.LineItems).NotEmpty();
    }
}

public class CreateInvoiceCommandHandler(IInvoiceRepository repo) : IRequestHandler<CreateInvoiceCommand, int>
{
    public async Task<int> Handle(CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var lineItems = cmd.LineItems.Select(i => new InvoiceLineItem
        {
            Description = i.Description,
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice,
            Amount      = i.Quantity * i.UnitPrice,
            GstRate     = i.GstRate,
            GstAmount   = Math.Round(i.Quantity * i.UnitPrice * i.GstRate / 100, 2),
        }).ToList();

        var subtotal  = lineItems.Sum(i => i.Amount);
        var gst       = lineItems.Sum(i => i.GstAmount);
        var total     = subtotal - cmd.DiscountAmount + gst;

        var invoice = new Invoice
        {
            InvoiceNumber  = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ReservationId  = cmd.ReservationId,
            GuestId        = cmd.GuestId,
            DueDate        = cmd.DueDate,
            Subtotal       = subtotal,
            DiscountAmount = cmd.DiscountAmount,
            GstAmount      = gst,
            TotalAmount    = total,
            BalanceDue     = total,
            Status         = InvoiceStatus.Pending,
            PaymentMethod  = cmd.PaymentMethod,
            Notes          = cmd.Notes,
        };
        return await repo.CreateAsync(invoice, lineItems, ct);
    }
}
