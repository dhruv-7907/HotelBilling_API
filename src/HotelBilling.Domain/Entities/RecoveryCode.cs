using System;

namespace HotelBilling.Domain.Entities;

/// <summary>
/// Domain entity representing a backup recovery code for 2FA.
/// </summary>
public class RecoveryCode
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>The identifier of the user associated with this recovery code.</summary>
    public int UserId { get; set; }

    /// <summary>The hashed value of the recovery code.</summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>Indicates whether the recovery code has been used.</summary>
    public bool Used { get; set; }

    /// <summary>The timestamp when the recovery code was used, if applicable.</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>The timestamp when the recovery code was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
