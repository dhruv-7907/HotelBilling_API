using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Application.Common.Interfaces;
public interface IInvoiceRepository
{
    Task<PagedResult<Invoice>> GetAllAsync(PaginationQuery query, string? status, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<InvoiceLineItem>> GetLineItemsAsync(int invoiceId, CancellationToken ct = default);
    Task<int>  CreateAsync(Invoice invoice, IEnumerable<InvoiceLineItem> items, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int id, InvoiceStatus status, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
