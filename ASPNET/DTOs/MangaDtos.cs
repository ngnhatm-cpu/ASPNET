namespace ASPNET.DTOs;

public class MangaDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? CoverImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public List<ChapterDto> Chapters { get; set; } = new();
}

public class ChapterDto
{
    public int Id { get; set; }
    public int MangaId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}
