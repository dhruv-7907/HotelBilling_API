using HotelBilling.Domain.Common;
namespace HotelBilling.Domain.Entities;
public class InvoiceLineItem : BaseEntity
{
    public int InvoiceId      { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity       { get; set; } = 1;
    public decimal UnitPrice  { get; set; }
    public decimal Amount     { get; set; }
    public decimal GstRate    { get; set; } = 18;
    public decimal GstAmount  { get; set; }
}
