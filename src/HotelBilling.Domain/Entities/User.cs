using HotelBilling.Domain.Common;
using HotelBilling.Domain.Enums;
namespace HotelBilling.Domain.Entities;
public class User : BaseEntity
{
    public string FullName    { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public string Phone       { get; set; } = string.Empty;
    public string PasswordHash{ get; set; } = string.Empty;
    public UserRole Role      { get; set; }
    public bool IsActive      { get; set; } = true;
    public string? RefreshToken           { get; set; }
    public DateTime? RefreshTokenExpiry   { get; set; }
    public DateTime? LastLoginAt          { get; set; }
}
