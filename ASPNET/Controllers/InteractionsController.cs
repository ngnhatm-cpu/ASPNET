using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InteractionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public InteractionsController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/Interactions/comment
    [HttpPost("comment")]
    [Authorize]
    public async Task<IActionResult> PostComment([FromBody] CommentRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Nội dung bình luận không được để trống" });

        var comment = new Comment
        {
            UserId = userId,
            ChapterId = request.ChapterId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Return comment with user info
        var result = await _context.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == comment.Id);

        return Ok(new {
            id = result!.Id,
            content = result.Content,
            createdAt = result.CreatedAt,
            username = result.User?.Username
        });
    }

    // GET: api/Interactions/comments/{chapterId}
    [HttpGet("comments/{chapterId}")]
    public async Task<IActionResult> GetComments(int chapterId)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ChapterId == chapterId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new {
                id = c.Id,
                content = c.Content,
                createdAt = c.CreatedAt,
                username = c.User!.Username
            })
            .ToListAsync();

        return Ok(comments);
    }

    // POST: api/Interactions/rate
    [HttpPost("rate")]
    [Authorize]
    public async Task<IActionResult> RateManga([FromBody] RatingRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (request.Stars < 1 || request.Stars > 5)
            return BadRequest(new { message = "Điểm đánh giá từ 1 đến 5 sao" });

        var existingRating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MangaId == request.MangaId);

        if (existingRating != null)
        {
            existingRating.Stars = request.Stars;
            existingRating.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var rating = new Rating
            {
                UserId = userId,
                MangaId = request.MangaId,
                Stars = request.Stars,
                CreatedAt = DateTime.UtcNow
            };
            _context.Ratings.Add(rating);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đánh giá thành công" });
    }

    // GET: api/Interactions/rating/{mangaId}
    [HttpGet("rating/{mangaId}")]
    public async Task<IActionResult> GetAverageRating(int mangaId)
    {
        var ratings = await _context.Ratings
            .Where(r => r.MangaId == mangaId)
            .ToListAsync();

        if (ratings.Count == 0)
            return Ok(new { average = 0, count = 0 });

        var average = Math.Round(ratings.Average(r => r.Stars), 1);
        return Ok(new { average, count = ratings.Count });
    }
}

public class CommentRequest
{
    public int ChapterId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class RatingRequest
{
    public int MangaId { get; set; }
    public int Stars { get; set; }
}
