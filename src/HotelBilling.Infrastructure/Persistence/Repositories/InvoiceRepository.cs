using Dapper;
using HotelBilling.Application.Common.Interfaces;
using HotelBilling.Application.Common.Models;
using HotelBilling.Domain.Entities;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(DapperContext ctx) : IInvoiceRepository
{
    public async Task<PagedResult<Invoice>> GetAllAsync(PaginationQuery query, string? status, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        var conditions = new List<string> { "i.IsDeleted = 0" };
        var statusId = ParseEnumOrNumeric<InvoiceStatus>(status);
        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add(statusId.HasValue ? "i.Status = @StatusId" : "1 = 0");
        if (!string.IsNullOrEmpty(query.Search))
            conditions.Add("(g.FullName LIKE @Search OR i.InvoiceNumber LIKE @Search)");

        var where = "WHERE " + string.Join(" AND ", conditions);
        var p = new { StatusId = statusId, Search = $"%{query.Search}%", Offset = (query.Page-1)*query.PageSize, query.PageSize };

        var countSql = $"SELECT COUNT(*) FROM Invoices i JOIN Guests g ON i.GuestId=g.Id {where}";
        var dataSql  = $@"
            SELECT i.*, g.FullName AS GuestName
            FROM Invoices i JOIN Guests g ON i.GuestId=g.Id
            {where}
            ORDER BY i.InvoiceDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<Invoice>(dataSql, p);
        return new PagedResult<Invoice> { Items = items, TotalCount = total, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        const string sql = @"SELECT i.*, g.FullName AS GuestName
                             FROM Invoices i JOIN Guests g ON i.GuestId=g.Id
                             WHERE i.Id=@Id AND i.IsDeleted=0";
        return await conn.QueryFirstOrDefaultAsync<Invoice>(sql, new { Id = id });
    }

    public async Task<IEnumerable<InvoiceLineItem>> GetLineItemsAsync(int invoiceId, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.QueryAsync<InvoiceLineItem>(
            "SELECT * FROM InvoiceLineItems WHERE InvoiceId=@InvoiceId AND IsDeleted=0", new { InvoiceId = invoiceId });
    }

    public async Task<int> CreateAsync(Invoice invoice, IEnumerable<InvoiceLineItem> items, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            const string invSql = @"
                INSERT INTO Invoices (InvoiceNumber,ReservationId,GuestId,InvoiceDate,DueDate,Subtotal,
                    DiscountAmount,GstAmount,TotalAmount,BalanceDue,Status,PaymentMethod,Notes,CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@InvoiceNumber,@ReservationId,@GuestId,@InvoiceDate,@DueDate,@Subtotal,
                    @DiscountAmount,@GstAmount,@TotalAmount,@BalanceDue,@Status,@PaymentMethod,@Notes,@CreatedAt)";
            var invoiceId = await conn.ExecuteScalarAsync<int>(invSql, invoice, tx);

            const string lineSql = @"
                INSERT INTO InvoiceLineItems (InvoiceId,Description,Quantity,UnitPrice,Amount,GstRate,GstAmount,CreatedAt)
                VALUES (@InvoiceId,@Description,@Quantity,@UnitPrice,@Amount,@GstRate,@GstAmount,@CreatedAt)";
            foreach (var item in items) { item.InvoiceId = invoiceId; item.CreatedAt = DateTime.UtcNow; }
            await conn.ExecuteAsync(lineSql, items, tx);

            tx.Commit();
            return invoiceId;
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task<bool> UpdateStatusAsync(int id, InvoiceStatus status, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        var paidOn = status == InvoiceStatus.Paid ? (DateTime?)DateTime.UtcNow : null;
        return await conn.ExecuteAsync(
            "UPDATE Invoices SET Status=@Status, PaidOn=@PaidOn, UpdatedAt=@Now WHERE Id=@Id",
            new { Status = (int)status, PaidOn = paidOn, Now = DateTime.UtcNow, Id = id }) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = ctx.CreateConnection();
        return await conn.ExecuteAsync("UPDATE Invoices SET IsDeleted=1, UpdatedAt=@Now WHERE Id=@Id",
            new { Now = DateTime.UtcNow, Id = id }) > 0;
    }

    private static int? ParseEnumOrNumeric<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(TEnum), numeric)) return numeric;
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? Convert.ToInt32(parsed) : null;
    }
}
