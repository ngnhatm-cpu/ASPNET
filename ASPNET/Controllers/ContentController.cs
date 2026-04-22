using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.Models;
using ASPNET.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.IO.Compression;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWatermarkService _watermarkService;
    private readonly IWebHostEnvironment _env;

    public ContentController(AppDbContext context, IWatermarkService watermarkService, IWebHostEnvironment env)
    {
        _context = context;
        _watermarkService = watermarkService;
        _env = env;
    }

    // GET: api/content/read/5
    [HttpGet("read/{chapterId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ReadChapter(int chapterId)
    {
        var chapter = await _context.Chapters.FindAsync(chapterId);
        if (chapter == null) return NotFound();

        var mangaId = chapter.MangaId;
        var currentOrder = chapter.OrderIndex;

        var prevChapterId = await _context.Chapters
            .Where(c => c.MangaId == mangaId && c.OrderIndex < currentOrder)
            .OrderByDescending(c => c.OrderIndex)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        var nextChapterId = await _context.Chapters
            .Where(c => c.MangaId == mangaId && c.OrderIndex > currentOrder)
            .OrderBy(c => c.OrderIndex)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        // 1. Nếu chương MIỄN PHÍ (Price = 0) -> Cho đọc luôn
        if (chapter.Price == 0)
        {
            return Ok(new { 
                mangaId = chapter.MangaId,
                chapterTitle = chapter.Title,
                isFree = true,
                pages = GetDemoPages(chapter.FilePath),
                prevChapterId,
                nextChapterId
            });
        }

        // 2. Nếu chương CÓ PHÍ -> Phải đăng nhập và đã mua
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { message = "Chương này yêu cầu đăng nhập và mua để đọc" });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var hasAccess = await _context.UserLibraries.AnyAsync(ul => ul.UserId == userId && ul.ChapterId == chapterId);
        
        if (!hasAccess)
        {
            // Trả về 403 kèm thông tin cơ bản để hiện Paywall trong Reader
            return StatusCode(403, new { 
                message = "Bạn chưa sở hữu chương này",
                mangaId = chapter.MangaId,
                chapterTitle = chapter.Title,
                price = chapter.Price,
                prevChapterId,
                nextChapterId
            });
        }

        return Ok(new { 
            mangaId = chapter.MangaId,
            chapterTitle = chapter.Title,
            isFree = false,
            pages = GetDemoPages(chapter.FilePath),
            prevChapterId,
            nextChapterId,
            expiresIn = "5 minutes"
        });
    }

    // GET: api/content/download/{chapterId}
    [HttpGet("download/{chapterId}")]
    public async Task<IActionResult> DownloadChapter(int chapterId)
    {
        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == chapterId);
        if (chapter == null) return NotFound();

        // Miễn phí thì cho tải luôn, có phí thì check library
        if (chapter.Price > 0)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var hasAccess = await _context.UserLibraries.AnyAsync(ul => ul.UserId == userId && ul.ChapterId == chapterId);
            if (!hasAccess) return Forbid();
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Guest";
        var imageUrls = GetDemoPages(chapter.FilePath);
        
        if (imageUrls == null || !imageUrls.Any())
            return BadRequest(new { message = "Chương này chưa có hình ảnh để tải." });

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var httpClient = new HttpClient();
            int pageNum = 1;

            foreach (var url in imageUrls)
            {
                byte[]? originalBytes = null;

                try 
                {
                    if (url.StartsWith("http"))
                    {
                        originalBytes = await httpClient.GetByteArrayAsync(url);
                    }
                    else 
                    {
                        // Local path
                        var physicalPath = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath))
                        {
                            originalBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
                        }
                    }

                    if (originalBytes != null)
                    {
                        // Đóng dấu Watermark
                        byte[] watermarkedImage = _watermarkService.ApplyWatermark(originalBytes, $"Bản quyền thuộc về: {userEmail}");
                        
                        var entry = archive.CreateEntry($"page_{pageNum++:D3}.jpg");
                        using var entryStream = entry.Open();
                        entryStream.Write(watermarkedImage, 0, watermarkedImage.Length);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other pages
                    Console.WriteLine($"Error processing page {url}: {ex.Message}");
                }
            }
        }

        if (ms.Length == 0)
            return BadRequest(new { message = "Không thể xử lý hình ảnh của chương này." });

        ms.Position = 0;
        var fileName = $"{chapter.Manga!.Title}_{chapter.Title}".Replace(" ", "_") + ".zip";
        return File(ms.ToArray(), "application/zip", fileName);
    }

    private List<string> GetDemoPages(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new List<string>();

        // Thử parse JSON array of URLs (định dạng Cloudinary mới)
        if (filePath.TrimStart().StartsWith("["))
        {
            try
            {
                var urls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(filePath);
                return urls ?? new List<string>();
            }
            catch { }
        }

        // Fallback: định dạng cũ (folder local path)
        var cleanPath = filePath.Replace('\\', '/').TrimStart('/');
        var physicalPath = Path.Combine(_env.WebRootPath, cleanPath);

        if (Directory.Exists(physicalPath))
        {
            return Directory.GetFiles(physicalPath)
                .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg") || f.EndsWith(".webp"))
                .OrderBy(f => f)
                .Select(f => "/" + cleanPath + "/" + Path.GetFileName(f))
                .ToList();
        }

        return new List<string>();
    }
}
