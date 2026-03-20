using System.ComponentModel.DataAnnotations;

namespace ASPNET.Models;

public class UserLibrary
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
}
