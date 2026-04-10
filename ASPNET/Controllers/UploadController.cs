using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Không có file nào được tải lên." });

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "mangas");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var imageUrl = $"/uploads/mangas/{uniqueFileName}";
        return Ok(new { url = imageUrl });
    }

    // POST: api/Upload/chapter-pages/{chapterId}
    // Upload ảnh trang truyện cho một chapter cụ thể (dùng chapterId làm thư mục cố định)
    [HttpPost("chapter-pages/{chapterId}")]
    public async Task<IActionResult> UploadChapterPages(int chapterId, [FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "Không có ảnh nào được chọn." });

        try
        {
            // Dùng chapterId cố định làm tên thư mục → không bao giờ sai đường dẫn
            var relativePath = Path.Combine("uploads", "chapters", chapterId.ToString());
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

            // Xóa ảnh cũ nếu có → luôn sạch trước khi upload mới
            if (Directory.Exists(physicalPath))
                Directory.Delete(physicalPath, recursive: true);

            Directory.CreateDirectory(physicalPath);

            int index = 1;
            foreach (var file in files.OrderBy(f => f.FileName))
            {
                var ext = Path.GetExtension(file.FileName);
                var pageFileName = $"{index:D3}{ext}"; // 001.jpg, 002.jpg ...
                var pagePath = Path.Combine(physicalPath, pageFileName);

                using (var stream = new FileStream(pagePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                index++;
            }

            // Đường dẫn virtual trả về cho client
            var virtualPath = "/" + relativePath.Replace('\\', '/');
            return Ok(new { folderUrl = virtualPath, count = files.Count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi Server khi tải ảnh: " + ex.Message });
        }
    }
}
