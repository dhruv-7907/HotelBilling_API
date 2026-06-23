namespace HotelBilling.Application.Common.Models;
public record PaginationQuery(int Page = 1, int PageSize = 10, string? Search = null, string? SortBy = null, bool SortDesc = false);
