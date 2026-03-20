using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.Models;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        return await _context.Categories
            .Include(c => c.Mangas)
            .ToListAsync();
    }

    // GET: api/categories/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Mangas)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound(new { message = $"Không tìm thấy thể loại có Id = {id}" });

        return category;
    }

    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<Category>> PostCategory(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    // PUT: api/categories/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCategory(int id, Category category)
    {
        if (id != category.Id)
            return BadRequest(new { message = "Id trong URL không khớp với Id trong body" });

        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Categories.Any(e => e.Id == id))
                return NotFound(new { message = $"Thể loại Id = {id} không tồn tại" });
            throw;
        }

        return NoContent();
    }

    // DELETE: api/categories/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = $"Không tìm thấy thể loại Id = {id}" });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa thể loại '{category.Name}'" });
    }
}
