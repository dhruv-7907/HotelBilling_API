using HotelBilling.Domain.Common;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Domain.Entities;
public class Invoice : BaseEntity
{
    public string InvoiceNumber    { get; set; } = string.Empty;
    public int ReservationId       { get; set; }
    public int GuestId             { get; set; }
    public DateTime InvoiceDate    { get; set; } = DateTime.UtcNow;
    public DateTime DueDate        { get; set; }
    public decimal Subtotal        { get; set; }
    public decimal DiscountAmount  { get; set; } = 0;
    public decimal GstAmount       { get; set; }
    public decimal TotalAmount     { get; set; }
    public decimal PaidAmount      { get; set; } = 0;
    public decimal BalanceDue      { get; set; }
    public InvoiceStatus Status    { get; set; } = InvoiceStatus.Draft;
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? PaidOn        { get; set; }
    public string? Notes           { get; set; }
}
