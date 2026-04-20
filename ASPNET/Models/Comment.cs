using System.ComponentModel.DataAnnotations;

namespace ASPNET.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
}
