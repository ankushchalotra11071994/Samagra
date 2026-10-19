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
    private const string CatalogCacheKey = "products:all:hybrid";

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
            CatalogCacheKey,
            async token => await _db.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)   // नए (imported) products ऊपर
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

    // GET /api/products/manage — admin list: includes inactive, stock aur paging
    [HttpGet("manage")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetForManagement(
        [FromQuery] string? q,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool includeInactive = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Products.AsNoTracking();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        if (categoryId is not null)
            query = query.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.Stock,
                p.IsActive,
                p.ImageUrl,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items, page, pageSize, total });
    }

    // GET /api/products/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Shop ko sirf active product dikhta hai; admin ko deactivated bhi,
        // warna uska edit form hi nahi khulega.
        var isAdmin = User.IsInRole("Admin");

        var p = await _db.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && (x.IsActive || isAdmin));

        if (p is null) return NotFound();

        return Ok(new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.Stock,
            p.IsActive,
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
            CategoryId  = request.CategoryId,
            ImageUrl    = request.ImageUrl
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        await InvalidateCatalogAsync();

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

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == request.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "Invalid category" });

        product.Name        = request.Name;
        product.Description = request.Description;
        product.Price       = request.Price;
        product.Stock       = request.Stock;
        product.CategoryId  = request.CategoryId;
        product.ImageUrl    = request.ImageUrl;

        await _db.SaveChangesAsync();
        await InvalidateCatalogAsync();
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
        await InvalidateCatalogAsync();
        return NoContent();
    }

    // POST /api/products/{id}/restore — soft delete ko wapas active karo
    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        product.IsActive = true;
        await _db.SaveChangesAsync();
        await InvalidateCatalogAsync();
        return NoContent();
    }

    // Catalog 30 सेकंड cache होता है — admin के edit तुरंत दिखें इसलिए key हटाते हैं.
    private ValueTask InvalidateCatalogAsync() =>
        _hybridCache.RemoveAsync(CatalogCacheKey);
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
    Guid CategoryId,
    string? ImageUrl = null
);
