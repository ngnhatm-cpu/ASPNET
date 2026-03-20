using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.Models;
using ASPNET.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Yêu cầu đăng nhập cho mọi thao tác đơn hàng
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/orders (Admin Only or User's own)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        IQueryable<Order> query = _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Chapter)
                    .ThenInclude(c => c!.Manga);

        if (role != "Admin")
        {
            query = query.Where(o => o.UserId == userId);
        }

        var orders = await query
            .Select(o => MapToDto(o))
            .ToListAsync();

        return Ok(orders);
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<OrderDto>> PostOrder([FromBody] CreateOrderRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (request.ChapterIds == null || request.ChapterIds.Count == 0)
            return BadRequest(new { message = "Đơn hàng phải có ít nhất 1 chương truyện" });

        // Lấy danh sách chương
        var chapters = await _context.Chapters
            .Include(c => c.Manga)
            .Where(c => request.ChapterIds.Contains(c.Id))
            .ToListAsync();

        if (chapters.Count != request.ChapterIds.Count)
            return BadRequest(new { message = "Một số chương truyện không tồn tại" });

        // Kiểm tra xem đã sở hữu chưa
        var existingOwnerships = await _context.UserLibraries
            .Where(ul => ul.UserId == userId && request.ChapterIds.Contains(ul.ChapterId))
            .Select(ul => ul.ChapterId)
            .ToListAsync();

        if (existingOwnerships.Any())
            return BadRequest(new { message = $"Bạn đã sở hữu các chương: {string.Join(", ", existingOwnerships)}" });

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = "Completed", // Truyện số thanh toán xong là sở hữu luôn
            OrderItems = new List<OrderItem>()
        };

        decimal total = 0;
        foreach (var chapter in chapters)
        {
            order.OrderItems.Add(new OrderItem
            {
                ChapterId = chapter.Id,
                UnitPrice = chapter.Price
            });
            total += chapter.Price;
        }

        order.TotalAmount = total;

        _context.Orders.Add(order);

        // GHI VÀO USER LIBRARY (Sổ đỏ sở hữu)
        foreach (var chapter in chapters)
        {
            _context.UserLibraries.Add(new UserLibrary
            {
                UserId = userId,
                ChapterId = chapter.Id
            });
        }

        await _context.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    private static OrderDto MapToDto(Order o)
    {
        return new OrderDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            Items = o.OrderItems.Select(oi => new OrderItemDto
            {
                ChapterId = oi.ChapterId,
                ChapterTitle = oi.Chapter?.Title ?? "N/A",
                MangaTitle = oi.Chapter?.Manga?.Title ?? "N/A",
                UnitPrice = oi.UnitPrice
            }).ToList()
        };
    }
}
