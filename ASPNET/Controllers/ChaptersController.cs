using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.Models;
using ASPNET.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChaptersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChaptersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/mangas/{mangaId}/chapters
    [HttpGet("/api/mangas/{mangaId}/chapters")]
    public async Task<ActionResult<IEnumerable<ChapterDto>>> GetChaptersByManga(int mangaId)
    {
        var chapters = await _context.Chapters
            .Where(c => c.MangaId == mangaId)
            .OrderBy(c => c.OrderIndex)
            .Select(c => MapToDto(c))
            .ToListAsync();

        return Ok(chapters);
    }

    // GET: api/chapters/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ChapterDto>> GetChapter(int id)
    {
        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter == null)
            return NotFound();

        return Ok(MapToDto(chapter));
    }

    // POST: api/chapters (Admin Only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChapterDto>> PostChapter(Chapter chapter)
    {
        if (!_context.Mangas.Any(m => m.Id == chapter.MangaId))
            return BadRequest(new { message = "Manga không tồn tại" });

        _context.Chapters.Add(chapter);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetChapter), new { id = chapter.Id }, MapToDto(chapter));
    }

    // DELETE: api/chapters/5 (Admin Only)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteChapter(int id)
    {
        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter == null)
            return NotFound();

        _context.Chapters.Remove(chapter);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ChapterDto MapToDto(Chapter c)
    {
        return new ChapterDto
        {
            Id = c.Id,
            MangaId = c.MangaId,
            Title = c.Title,
            Price = c.Price,
            OrderIndex = c.OrderIndex,
            CreatedAt = c.CreatedAt
        };
    }
}
