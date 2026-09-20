 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;
using Microsoft.Extensions.Caching.Hybrid;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly HybridCache _hybridCache;

    public ProductsController(AppDbContext db, HybridCache hybridCache)
    {
        _db = db;
        _hybridCache = hybridCache;
    }

    // GET /api/products/hybrid
    [HttpGet("hybrid")]
    public async Task<IActionResult> GetAllHybridCached(CancellationToken ct)
    {
        var products = await _hybridCache.GetOrCreateAsync(
            "products:all:hybrid",
            async token => await _db.Products
                .Where(p => p.IsActive)
                .Select(p => new ProductDto(
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Category.Name,
                    p.ImageUrl))        // ← ImageUrl add kiya
                .Take(100)
                .ToListAsync(token),
            cancellationToken: ct);

        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var p = await _db.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (p is null) return NotFound();

        return Ok(new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.Stock,
            p.CategoryId,
            CategoryName = p.Category.Name,
            p.ImageUrl                  // ← ImageUrl add kiya
        });
    }

    // POST /api/products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == request.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "Invalid category" });

        var product = new Product
        {
            Id          = Guid.NewGuid(),
            Name        = request.Name,
            Description = request.Description,
            Price       = request.Price,
            Stock       = request.Stock,
            CategoryId  = request.CategoryId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), 
            new { id = product.Id }, 
            new { product.Id });
    }

    // PUT /api/products/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, 
        [FromBody] CreateProductRequest request)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        product.Name        = request.Name;
        product.Description = request.Description;
        product.Price       = request.Price;
        product.Stock       = request.Stock;
        product.CategoryId  = request.CategoryId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/products/{id} — soft delete
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        product.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

// ← ImageUrl add kiya
public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string CategoryName,
    string? ImageUrl
);

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid CategoryId
);