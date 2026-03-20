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
    public async Task<IActionResult> ReadChapter(int chapterId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Kiểm tra quyền sở hữu
        var hasAccess = await _context.UserLibraries.AnyAsync(ul => ul.UserId == userId && ul.ChapterId == chapterId);
        if (!hasAccess)
            return Forbid(); // Trả về 403 nếu chưa mua

        var chapter = await _context.Chapters.FindAsync(chapterId);
        if (chapter == null) return NotFound();

        // Giả lập trả về danh sách link ảnh có chữ ký (Pre-signed URLs)
        // Trong thực tế sẽ gọi AWS S3 SDK hoặc Azure Blob SDK
        var demoPages = new List<string> {
            $"https://storage.cdn.com/{chapter.FilePath}/page1.jpg?token=signed_exp_5min",
            $"https://storage.cdn.com/{chapter.FilePath}/page2.jpg?token=signed_exp_5min"
        };

        return Ok(new { 
            chapterTitle = chapter.Title,
            pages = demoPages,
            expiresIn = "5 minutes"
        });
    }

    // GET: api/content/download/{chapterId}
    [HttpGet("download/{chapterId}")]
    public async Task<IActionResult> DownloadChapter(int chapterId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

        // Check ownership
        var hasAccess = await _context.UserLibraries.AnyAsync(ul => ul.UserId == userId && ul.ChapterId == chapterId);
        if (!hasAccess) return Forbid();

        var chapter = await _context.Chapters.Include(c => c.Manga).FirstOrDefaultAsync(c => c.Id == chapterId);
        if (chapter == null) return NotFound();

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

    private byte[] CreateDummyImage()
    {
        // Tạo 1 ảnh JPEG trắng đơn giản để demo watermark
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(800, 1200);
        using var outMs = new MemoryStream();
        image.Save(outMs, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        return outMs.ToArray();
    }
}
