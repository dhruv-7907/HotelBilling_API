using MediatR;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Features.Invoices.Queries;
public record GetInvoicesQuery(int Page=1, int PageSize=10, string? Search=null, string? Status=null) : IRequest<PagedResult<Invoice>>;
public class GetInvoicesQueryHandler(IInvoiceRepository repo) : IRequestHandler<GetInvoicesQuery, PagedResult<Invoice>>
{
    public Task<PagedResult<Invoice>> Handle(GetInvoicesQuery q, CancellationToken ct)
        => repo.GetAllAsync(new PaginationQuery(q.Page, q.PageSize, q.Search), q.Status, ct);
}
