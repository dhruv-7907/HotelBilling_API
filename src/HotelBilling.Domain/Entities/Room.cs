using HotelBilling.Domain.Common;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Domain.Entities;
public class Room : BaseEntity
{
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int Floor         { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public decimal RatePerNight { get; set; }
    public int MaxOccupancy  { get; set; } = 2;
    public string? Description { get; set; }
    public bool HasMinibar   { get; set; }
    public bool HasJacuzzi   { get; set; }
    public string? Notes     { get; set; }
}
