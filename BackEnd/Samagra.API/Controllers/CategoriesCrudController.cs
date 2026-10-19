// Samagra.API/Controllers/CategoriesCrudController.cs
//
// CategoriesController.cs is a LINQ join playground, so the real category API
// lives here. Both share the /api/categories prefix — the demo actions all sit
// on suffixed routes ("inner-join", "summary", …), these on the bare resource.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesCrudController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesCrudController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/categories — shop filter aur admin list dono isi se chalte hain
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = _db.Categories.AsNoTracking();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.IsActive,
                c.CreatedAt,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync(ct);

        return Ok(categories);
    }

    // GET /api/categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.IsActive,
                c.CreatedAt,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        return category is null ? NotFound() : Ok(category);
    }

    // POST /api/categories
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] SaveCategoryRequest request,
        CancellationToken ct)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Name is required" });

        if (await _db.Categories.AnyAsync(c => c.Name == name, ct))
            return Conflict(new { message = $"A category named '{name}' already exists" });

        var category = new Category
        {
            Id       = Guid.NewGuid(),
            Name     = name,
            IsActive = request.IsActive
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, new { category.Id });
    }

    // PUT /api/categories/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SaveCategoryRequest request,
        CancellationToken ct)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Name is required" });

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        if (await _db.Categories.AnyAsync(c => c.Name == name && c.Id != id, ct))
            return Conflict(new { message = $"A category named '{name}' already exists" });

        category.Name     = name;
        category.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/categories/{id} — soft delete, products ka FK required hai
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        var activeProducts = await _db.Products
            .CountAsync(p => p.CategoryId == id && p.IsActive, ct);

        if (activeProducts > 0)
            return BadRequest(new
            {
                message = $"{activeProducts} active product(s) still use this category"
            });

        category.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record SaveCategoryRequest(string? Name, bool IsActive = true);
