using HotelBilling.Domain.Common;
namespace HotelBilling.Domain.Entities;
public class HousekeepingTask : BaseEntity
{
    public int RoomId          { get; set; }
    public int? AssignedToId   { get; set; }
    public string TaskType     { get; set; } = string.Empty;  // Clean, Turndown, Inspection
    public string Status       { get; set; } = "Pending";
    public string? Notes       { get; set; }
    public DateTime? CompletedAt { get; set; }
}
