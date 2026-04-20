using System.ComponentModel.DataAnnotations;

namespace ASPNET.Models;

public class Rating
{
    public int Id { get; set; }

    [Required]
    [Range(1, 5)]
    public int Stars { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int MangaId { get; set; }
    public Manga? Manga { get; set; }
}
