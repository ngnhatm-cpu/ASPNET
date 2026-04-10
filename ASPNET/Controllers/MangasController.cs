using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.Models;
using ASPNET.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MangasController : ControllerBase
{
    private readonly AppDbContext _context;

    public MangasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/mangas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MangaDto>>> GetMangas()
    {
        var mangas = await _context.Mangas
            .Include(m => m.Category)
            .Include(m => m.Chapters)
            .Select(m => MapToDto(m, null))
            .ToListAsync();

        return Ok(mangas);
    }

    // GET: api/mangas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<MangaDto>> GetManga(int id)
    {
        var manga = await _context.Mangas
            .Include(m => m.Category)
            .Include(m => m.Chapters)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (manga == null)
            return NotFound(new { message = $"Không tìm thấy truyện có Id = {id}" });

        // Check purchased chapters if logged in
        HashSet<int> purchasedChapterIds = new();
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            var purchasedList = await _context.OrderItems
                .Where(oi => oi.Order!.UserId == userId && oi.Chapter!.MangaId == id)
                .Select(oi => oi.ChapterId)
                .ToListAsync();
            purchasedChapterIds = purchasedList.ToHashSet();
        }

        return Ok(MapToDto(manga, purchasedChapterIds));
    }

    // GET: api/mangas/category/3
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<MangaDto>>> GetMangasByCategory(int categoryId)
    {
        var mangas = await _context.Mangas
            .Include(m => m.Category)
            .Include(m => m.Chapters)
            .Where(m => m.CategoryId == categoryId)
            .Select(m => MapToDto(m, null))
            .ToListAsync();

        return Ok(mangas);
    }

    // GET: api/mangas/search?keyword=naruto
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<MangaDto>>> SearchMangas([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Vui lòng nhập từ khóa tìm kiếm" });

        var mangas = await _context.Mangas
            .Include(m => m.Category)
            .Include(m => m.Chapters)
            .Where(m => m.Title.Contains(keyword) || (m.Author != null && m.Author.Contains(keyword)))
            .Select(m => MapToDto(m, null))
            .ToListAsync();

        return Ok(mangas);
    }

    // POST: api/mangas (Admin Only)
    [HttpPost]
    public async Task<ActionResult<MangaDto>> PostManga(Manga manga)
    {
        if (!_context.Categories.Any(c => c.Id == manga.CategoryId))
            return BadRequest(new { message = $"Thể loại Id = {manga.CategoryId} không tồn tại" });

        _context.Mangas.Add(manga);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetManga), new { id = manga.Id }, MapToDto(manga));
    }

    // PUT: api/mangas/5 (Admin Only)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutManga(int id, Manga manga)
    {
        if (id != manga.Id)
            return BadRequest(new { message = "Id không khớp" });

        if (!_context.Categories.Any(c => c.Id == manga.CategoryId))
            return BadRequest(new { message = "Thể loại không tồn tại" });

        _context.Entry(manga).State = EntityState.Modified;

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Mangas.Any(e => e.Id == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE: api/mangas/5 (Admin Only)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteManga(int id)
    {
        var manga = await _context.Mangas.FindAsync(id);
        if (manga == null)
            return NotFound(new { message = $"Không tìm thấy truyện Id = {id}" });

        _context.Mangas.Remove(manga);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa truyện '{manga.Title}'" });
    }

    private static MangaDto MapToDto(Manga m, HashSet<int>? purchasedChapterIds = null)
    {
        purchasedChapterIds ??= new HashSet<int>();
        return new MangaDto
        {
            Id = m.Id,
            Title = m.Title,
            Author = m.Author,
            Description = m.Description,
            Price = m.Price,
            CoverImageUrl = m.CoverImageUrl,
            CategoryId = m.CategoryId,
            CategoryName = m.Category?.Name,
            Chapters = m.Chapters.Select(c => new ChapterDto
            {
                Id = c.Id,
                MangaId = c.MangaId,
                Title = c.Title,
                Price = c.Price,
                OrderIndex = c.OrderIndex,
                CreatedAt = c.CreatedAt,
                IsPurchased = purchasedChapterIds.Contains(c.Id)
            }).OrderBy(c => c.OrderIndex).ToList()
        };
    }
}
