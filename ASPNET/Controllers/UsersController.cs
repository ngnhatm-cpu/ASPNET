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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/users (Admin Only)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        //return await _context.Users
        //    .Select(u => MapToDto(u))
        //    .ToListAsync();
        return await _context.Users
    .Select(u => new UserDto
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        Role = u.Role,
        Balance = u.Balance,
        CreatedAt = u.CreatedAt
    })
    .ToListAsync();
    }

    // GET: api/users/5 (Admin or Self)
    [HttpGet("{id}")]
    [Authorize]
    //public async Task<ActionResult<UserDto>> GetUser(int id)
    //{
    //    var currentUserId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
    //    var currentUserRole = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role);

    //    if (currentUserRole != "Admin" && currentUserId != id)
    //        return Forbid();

    //    var user = await _context.Users
    //        .Include(u => u.Library)
    //        .FirstOrDefaultAsync(u => u.Id == id);

    //    if (user == null)
    //        return NotFound();

    //    return Ok(MapToDto(user));
    //}

    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

        // BẠN ĐANG THIẾU ĐOẠN NÀY (Đoạn này vừa check lỗi, vừa tạo ra biến currentUserId)
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized();
        }

        // Biến currentUserId ở trên giờ đã có giá trị để dùng ở đây rồi nè
        if (currentUserRole != "Admin" && currentUserId != id)
            return Forbid();

        var user = await _context.Users
            .Include(u => u.Library)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        return Ok(MapToDto(user));
    }

    // PUT: api/users/5 (Admin or Self)
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutUser(int id, User user)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

        if (currentUserRole != "Admin" && currentUserId != id)
            return Forbid();

        if (id != user.Id)
            return BadRequest();

        var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (existingUser == null) return NotFound();

        // Admin có thể đổi Role/Balance, còn User thường thì không
        if (currentUserRole != "Admin")
        {
            user.Role = existingUser.Role;
            user.Balance = existingUser.Balance;
        }
        
        // Luôn giữ PasswordHash cũ nếu không có cơ chế đổi mật khẩu riêng ở đây
        user.PasswordHash = existingUser.PasswordHash;
        user.CreatedAt = existingUser.CreatedAt;

        _context.Entry(user).State = EntityState.Modified;

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Users.Any(e => e.Id == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE: api/users/5 (Admin Only)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static UserDto MapToDto(User u)
    {
        return new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role,
            Balance = u.Balance,
            CreatedAt = u.CreatedAt
        };
    }
}
