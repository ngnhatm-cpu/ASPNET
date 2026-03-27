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

    public ContentController(AppDbContext context, IWatermarkService watermarkService)
    {
        _context = context;
        _watermarkService = watermarkService;
    }

    // GET: api/content/read/5
    [HttpGet("read/{chapterId}")]
    [AllowAnonymous] // Cho phép khách vãng lai đọc chương miễn phí
    public async Task<IActionResult> ReadChapter(int chapterId)
    {
        var chapter = await _context.Chapters.FindAsync(chapterId);
        if (chapter == null) return NotFound();

        // 1. Nếu chương MIỄN PHÍ (Price = 0) -> Cho đọc luôn
        if (chapter.Price == 0)
        {
            return Ok(new { 
                chapterTitle = chapter.Title,
                isFree = true,
                pages = GetDemoPages(chapter.FilePath)
            });
        }

        // 2. Nếu chương CÓ PHÍ -> Phải đăng nhập và đã mua
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { message = "Chương này yêu cầu đăng nhập và mua để đọc" });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var hasAccess = await _context.UserLibraries.AnyAsync(ul => ul.UserId == userId && ul.ChapterId == chapterId);
        
        if (!hasAccess)
            return Forbid(); // Trả về 403 nếu chương có phí mà chưa mua

        return Ok(new { 
            chapterTitle = chapter.Title,
            isFree = false,
            pages = GetDemoPages(chapter.FilePath),
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

        // 1. Giả lập lấy ảnh từ folder/cloud
        // Thực tế: Lấy ảnh từ đĩa hoặc cloud storage
        // Demo: Tạo 1 ảnh trống hoặc dùng ảnh mẫu
        byte[] dummyImage = CreateDummyImage();

        // 2. Đóng dấu Watermark email khách hàng
        byte[] watermarkedImage = _watermarkService.ApplyWatermark(dummyImage, $"Bản quyền thuộc về: {userEmail}");

        // 3. Nén thành file .zip
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry($"{chapter.Manga!.Title}_{chapter.Title}_page1.jpg");
            using var entryStream = entry.Open();
            entryStream.Write(watermarkedImage, 0, watermarkedImage.Length);
        }

        ms.Position = 0;
        return File(ms.ToArray(), "application/zip", $"{chapter.Manga!.Title}_{chapter.Title}.zip");
    }

    private List<string> GetDemoPages(string? filePath)
    {
        return new List<string> {
            $"https://storage.cdn.com/{filePath}/page1.jpg?token=signed_demo",
            $"https://storage.cdn.com/{filePath}/page2.jpg?token=signed_demo"
        };
    }

    private byte[] CreateDummyImage()
    {
        // Tạo 1 ảnh JPEG trắng đơn giản để demo watermark
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(800, 1200);
        using var outMs = new MemoryStream();
        image.Save(outMs, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        return outMs.ToArray();
    }
}
