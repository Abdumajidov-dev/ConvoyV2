using System.ComponentModel.DataAnnotations.Schema;

namespace Convoy.Domain.Entities;

/// <summary>
/// OTP kodlarni vaqtinchalik saqlash uchun entity
/// </summary>
[Table("otp_codes")]
public class OtpCode
{
    [Column("id")]
    public int Id { get; set; }

    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// OTP kod hali amal qilmoqdami?
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow <= ExpiresAt;
}
