using MediatR;
using HotelBilling.Application.Common.Exceptions;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Invoices.Queries;
public record InvoiceDetail(Invoice Invoice, IEnumerable<InvoiceLineItem> LineItems);
public record GetInvoiceByIdQuery(int Id) : IRequest<InvoiceDetail>;
public class GetInvoiceByIdQueryHandler(IInvoiceRepository repo) : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetail>
{
    public async Task<InvoiceDetail> Handle(GetInvoiceByIdQuery q, CancellationToken ct)
    {
        var inv   = await repo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException("Invoice", q.Id);
        var items = await repo.GetLineItemsAsync(q.Id, ct);
        return new InvoiceDetail(inv, items);
    }
}
