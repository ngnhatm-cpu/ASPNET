using System.ComponentModel.DataAnnotations;

namespace ASPNET.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    // Navigation property: 1 Category → nhiều Manga
    public ICollection<Manga> Mangas { get; set; } = new List<Manga>();
}
