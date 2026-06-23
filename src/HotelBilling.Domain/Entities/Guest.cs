using HotelBilling.Domain.Common;
namespace HotelBilling.Domain.Entities;
public class Guest : BaseEntity
{
    public string FullName     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string Phone        { get; set; } = string.Empty;
    public string? Address     { get; set; }
    public string? City        { get; set; }
    public string? Nationality { get; set; }
    public string? IdType      { get; set; }
    public string? IdNumber    { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsVip          { get; set; } = false;
    public int TotalStays      { get; set; } = 0;
    public decimal TotalSpent  { get; set; } = 0;
    public int RatingAvg       { get; set; } = 0;
    public string? Notes       { get; set; }
}
