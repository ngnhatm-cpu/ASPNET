using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var stats = new DashboardStatsDto();

        // 1. Tổng doanh thu (Từ các đơn hàng Completed)
        stats.TotalRevenue = await _context.Orders
            .Where(o => o.Status == "Completed")
            .SumAsync(o => o.TotalAmount);

        // 2. Số đơn hàng mới (Trong 24h qua)
        var yesterday = DateTime.UtcNow.AddDays(-1);
        stats.NewOrdersCount = await _context.Orders
            .CountAsync(o => o.OrderDate >= yesterday);

        // 3. Tổng số User
        stats.TotalUsersCount = await _context.Users.CountAsync();

        // 4. Tổng số Chương truyện (Digital content count)
        stats.TotalChaptersCount = await _context.Chapters.CountAsync();

        // 5. Lấy 5 đơn hàng gần nhất
        stats.RecentOrders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Chapter)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CustomerName = o.User!.Username,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    ChapterId = oi.ChapterId,
                    ChapterTitle = oi.Chapter!.Title,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(stats);
    }
}
