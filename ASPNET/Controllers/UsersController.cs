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
        return await _context.Users
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    // GET: api/users/5 (Admin or Self)
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
        var currentUserRole = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role);

        if (currentUserRole != "Admin" && currentUserId != id)
            return Forbid();

        var user = await _context.Users
            .Include(u => u.Library)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        return Ok(MapToDto(user));
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
            CreatedAt = u.CreatedAt
        };
    }
}
