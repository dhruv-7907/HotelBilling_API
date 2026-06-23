using HotelBilling.Domain.Common;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Domain.Entities;
public class Reservation : BaseEntity
{
    public string ReservationCode { get; set; } = string.Empty;
    public int GuestId        { get; set; }
    public int RoomId         { get; set; }
    public DateTime CheckIn   { get; set; }
    public DateTime CheckOut  { get; set; }
    public int Nights         { get; set; }
    public int Adults         { get; set; } = 1;
    public int Children       { get; set; } = 0;
    public decimal RatePerNight { get; set; }
    public decimal Subtotal   { get; set; }
    public decimal GstAmount  { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdvancePaid { get; set; } = 0;
    public decimal BalanceDue  { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public BookingChannel Channel   { get; set; } = BookingChannel.Direct;
    public PaymentMethod PaymentMethod { get; set; }
    public string? SpecialRequests { get; set; }
    public string? CancellationReason { get; set; }
}
