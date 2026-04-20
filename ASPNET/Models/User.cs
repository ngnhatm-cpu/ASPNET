using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = "Customer"; // Admin, Customer

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0; // Số dư Xu

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: 1 User → nhiều Order
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    // Navigation property: Thư viện sở hữu
    public ICollection<UserLibrary> Library { get; set; } = new List<UserLibrary>();

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
