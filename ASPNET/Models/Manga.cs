using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.Models;

public class Manga
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Author { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; } // Giá cả bộ (nếu có combo) hoặc giá tham khảo

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    // FK → Category
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Navigation property: Manga có nhiều chương
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
