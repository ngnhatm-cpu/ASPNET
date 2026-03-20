using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.Models;

public class Chapter
{
    public int Id { get; set; }

    [Required]
    public int MangaId { get; set; }
    public Manga? Manga { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty; // Thư mục lưu ảnh hoặc file zip

    public int OrderIndex { get; set; } // Tập 1, Tập 2...

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Danh sách người đã mua chương này
    public ICollection<UserLibrary> UserLibraries { get; set; } = new List<UserLibrary>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
