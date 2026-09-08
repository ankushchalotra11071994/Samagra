using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
namespace Samagra.API.Controllers; 

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _Inmemorycache;
     private readonly HybridCache _hybridCache;
    private readonly IDistributedCache _distributedCache;
    public ProductsController(AppDbContext db, 
    IMemoryCache Inmemorycache, 
    IDistributedCache distributedCache,HybridCache hybridCache)
    {
        _db = db;
        _Inmemorycache = Inmemorycache;
        _distributedCache = distributedCache;
        _hybridCache = hybridCache;
    }


    // GET /api/products
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Products.Where(x => x.IsActive == true).Take(200).ToListAsync();
        var result = new List<object>();



        foreach (var item in products)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == item.CategoryId);

            result.Add(new
            {
                item.Id,
                item.Name,
                Categoryname = category?.Name
            });




        }
        return Ok(result);
    }
    [HttpGet("memory")]
    public async Task<IActionResult> GetAllMemoryCached()
    {
        const string cacheKey = "products:all";

        var result = await _Inmemorycache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

            return await _db.Products
                .Where(p => p.IsActive)
                .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Category.Name))
                .Take(100)
                .ToListAsync();
        });

        return Ok(result);
    }

    [HttpGet("output")]
    [OutputCache(PolicyName = "Products30s")]
    public async Task<IActionResult> GetAllOutputCached()
    {
        var products = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Category.Name))
            .Take(100)
            .ToListAsync();

        return Ok(products);
    }

    // GET /api/products/distributed
    [HttpGet("distributed")]
    public async Task<IActionResult> GetAllDistributedCached()
    {
        const string cacheKey = "products:all";

        // 1. cache देखो
        var cached = await _distributedCache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            var fromCache = JsonSerializer.Deserialize<List<ProductDto>>(cached);
            return Ok(fromCache);
        }
        var products = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Category.Name))
            .Take(100)
            .ToListAsync();
        await _distributedCache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(products),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                    });
        return Ok(products);
    }
    [HttpGet("hybrid")]
    public async Task<IActionResult> GetAllHybridCached(CancellationToken ct)
    {
        var products = await _hybridCache.GetOrCreateAsync(
            "products:all:hybrid",
            async token => await _db.Products
                .Where(p => p.IsActive)
                .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Category.Name))
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
            CategoryName = p.Category.Name
        });
    }

    // POST /api/products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) return BadRequest(new { message = "Invalid category" });

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new { product.Id });
    }

    // PUT /api/products/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProductRequest request)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.CategoryId = request.CategoryId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/products/{id}  — soft delete
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        product.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
public record ProductDto(Guid Id, string Name, decimal Price, string CategoryName);
public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid CategoryId
);