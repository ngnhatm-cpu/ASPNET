using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UploadController : ControllerBase
{
    private readonly Cloudinary _cloudinary;

    public UploadController(IConfiguration config)
    {
        var cloudName = config["Cloudinary:CloudName"] ?? "dxomzjopo";
        var apiKey    = config["Cloudinary:ApiKey"]    ?? "579321641957572";
        var apiSecret = config["Cloudinary:ApiSecret"] ?? "jBSdfW7ukLu5kOFFw-zWPpJa7X8";

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    // POST: api/Upload/image
    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Không có file nào được tải lên." });

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File           = new FileDescription(file.FileName, stream),
            Folder         = "mangastore/mangas",
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            return BadRequest(new { message = result.Error.Message });

        return Ok(new { url = result.SecureUrl.ToString() });
    }

    // POST: api/Upload/chapter-pages/{chapterId}
    [HttpPost("chapter-pages/{chapterId}")]
    public async Task<IActionResult> UploadChapterPages(int chapterId, [FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "Không có ảnh nào được chọn." });

        try
        {
            var urls = new List<string>();
            int index = 1;

            foreach (var file in files.OrderBy(f => f.FileName))
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File           = new FileDescription($"{index:D3}{System.IO.Path.GetExtension(file.FileName)}", stream),
                    Folder         = $"mangastore/chapters/{chapterId}",
                    PublicId       = $"{index:D3}",
                    Overwrite      = true,
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                    return BadRequest(new { message = $"Lỗi upload trang {index}: {result.Error.Message}" });

                urls.Add(result.SecureUrl.ToString());
                index++;
            }

            return Ok(new { urls = urls, count = urls.Count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi Server khi tải ảnh: " + ex.Message });
        }
    }
}
