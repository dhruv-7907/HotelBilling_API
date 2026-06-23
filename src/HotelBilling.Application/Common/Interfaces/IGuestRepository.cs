using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
namespace HotelBilling.Application.Common.Interfaces;
public interface IGuestRepository
{
    Task<PagedResult<Guest>> GetAllAsync(PaginationQuery query, CancellationToken ct = default);
    Task<Guest?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Guest?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<int>  CreateAsync(Guest entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guest entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
