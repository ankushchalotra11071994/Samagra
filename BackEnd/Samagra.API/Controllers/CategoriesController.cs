using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samagra.Infrastructure.Data; // TODO: apne DbContext ka sahi namespace lagayein

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext  _context; // TODO: apne DbContext ka naam lagayein
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(AppDbContext context, ILogger<CategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =====================================================================
    // 1. INNER JOIN (Query Syntax)
    // Sirf wahi rows jo dono tables mein match karti hain.
    // Jis category mein koi product nahi, wo nahi aayegi.
    // SQL: SELECT ... FROM Categories c INNER JOIN Products p ON c.Id = p.CategoryId
    // =====================================================================
    [HttpGet("inner-join")]
    public async Task<IActionResult> InnerJoin()
    {
        var result = await (
            from c in _context.Categories.AsNoTracking()
            join p in _context.Products.AsNoTracking()
                on c.Id equals p.CategoryId          // '==' nahi, 'equals' likhna hai
            select new
            {
                CategoryName = c.Name,
                ProductName = p.Name,
                p.Price
            }).ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 2. INNER JOIN (Method Syntax)
    // 4 parameters: doosri table -> meri key -> uski key -> result
    // =====================================================================
    [HttpGet("inner-join-method")]
    public async Task<IActionResult> InnerJoinMethod()
    {
        var result = await _context.Categories.AsNoTracking()
            .Join(
                _context.Products.AsNoTracking(),
                c => c.Id,
                p => p.CategoryId,
                (c, p) => new
                {
                    CategoryName = c.Name,
                    ProductName = p.Name,
                    p.Price
                })
            .ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 3. NAVIGATION PROPERTY (EF Core ka sabse aasan tarika)
    // Join likhne ki zaroorat nahi, EF Core khud SQL JOIN banata hai.
    // Interview mein pehle yahi batayein.
    // =====================================================================
    [HttpGet("navigation")]
    public async Task<IActionResult> NavigationJoin()
    {
        var result = await _context.Products.AsNoTracking()
            .Select(p => new
            {
                ProductName = p.Name,
                CategoryName = p.Category.Name
            })
            .ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 4. LEFT JOIN (Query Syntax)
    // Saari categories aayengi, chahe product ho ya na ho.
    // Pattern yaad rakhein: join ... into  +  from ... DefaultIfEmpty()
    // SQL: ... LEFT JOIN Products p ON c.Id = p.CategoryId
    // =====================================================================
    [HttpGet("left-join")]
    public async Task<IActionResult> LeftJoin()
    {
        var result = await (
            from c in _context.Categories.AsNoTracking()
            join p in _context.Products.AsNoTracking()
                on c.Id equals p.CategoryId into categoryProducts
            from p in categoryProducts.DefaultIfEmpty()
            select new
            {
                CategoryName = c.Name,
                ProductName = p == null ? null : p.Name,
                // (decimal?) cast zaroori hai, warna null aane par
                // "Nullable object must have a value" exception aayega
                Price = p == null ? (decimal?)null : p.Price
            }).ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 5. LEFT JOIN (Method Syntax)
    // GroupJoin + SelectMany + DefaultIfEmpty
    // Note: .NET 10 / EF Core 10 mein seedha .LeftJoin() method bhi aa gaya hai.
    // =====================================================================
    [HttpGet("left-join-method")]
    public async Task<IActionResult> LeftJoinMethod()
    {
        var result = await _context.Categories.AsNoTracking()
            .GroupJoin(
                _context.Products.AsNoTracking(),
                c => c.Id,
                p => p.CategoryId,
                (c, products) => new { Category = c, Products = products })
            .SelectMany(
                x => x.Products.DefaultIfEmpty(),
                (x, p) => new
                {
                    CategoryName = x.Category.Name,
                    ProductName = p == null ? null : p.Name,
                    Price = p == null ? (decimal?)null : p.Price
                })
            .ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 6. GROUP JOIN (har category ke saath uske products ki list)
    // Dhyan dein: EF Core plain GroupJoin (bina SelectMany ke) ko SQL mein
    // translate NAHI karta, runtime exception deta hai.
    // Isliye navigation property se nested list banate hain.
    // =====================================================================
    [HttpGet("group-join")]
    public async Task<IActionResult> GroupJoin()
    {
        var result = await _context.Categories.AsNoTracking()
            .Select(c => new
            {
                c.Id,
                CategoryName = c.Name,
                Products = c.Products
                    .Where(p => p.IsActive)
                    .Select(p => new { p.Id, p.Name, p.Price })
                    .ToList()
            })
            .ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 7. JOIN + AGGREGATE (category-wise product count aur total stock)
    // SQL: LEFT JOIN + GROUP BY ka kaam
    // =====================================================================
    [HttpGet("summary")]
    public async Task<IActionResult> CategorySummary()
    {
        var result = await _context.Categories.AsNoTracking()
            .Select(c => new
            {
                CategoryName = c.Name,
                ProductCount = c.Products.Count(),
                TotalStock = c.Products.Sum(p => (int?)p.Stock) ?? 0,
                AvgPrice = c.Products.Average(p => (decimal?)p.Price)
            })
            .OrderByDescending(x => x.ProductCount)
            .ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 8. COMPOSITE KEY JOIN (multiple conditions par join)
    // Dono anonymous objects mein property ke NAAM aur TYPE same hone chahiye,
    // warna compile error aayega.
    // Yahan demo ke liye: Id match + IsActive dono taraf same.
    // Real use: jab table ki foreign key 2 columns ki ho.
    // SQL: ... ON c.Id = p.CategoryId AND c.IsActive = p.IsActive
    // =====================================================================
    [HttpGet("composite-join")]
    public async Task<IActionResult> CompositeJoin()
    {
        var result = await (
            from c in _context.Categories.AsNoTracking()
            join p in _context.Products.AsNoTracking()
                on new { CatId = c.Id, Active = c.IsActive }
                equals new { CatId = p.CategoryId, Active = p.IsActive }
            select new
            {
                CategoryName = c.Name,
                ProductName = p.Name,
                c.IsActive
            }).ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 9. RIGHT JOIN
    // LINQ mein purane versions mein RIGHT JOIN nahi hai, isliye tables ki
    // side badal kar LEFT JOIN likhte hain.
    // Imandaari ki baat: yahan CategoryId required FK hai, to har product ki
    // category hogi hi. Isliye result INNER JOIN jaisa hi aayega.
    // Fark tab dikhega jab FK nullable ho (Guid? CategoryId).
    // =====================================================================
    [HttpGet("right-join")]
    public async Task<IActionResult> RightJoin()
    {
        var result = await (
            from p in _context.Products.AsNoTracking()
            join c in _context.Categories.AsNoTracking()
                on p.CategoryId equals c.Id into productCategories
            from c in productCategories.DefaultIfEmpty()
            select new
            {
                ProductName = p.Name,
                CategoryName = c == null ? null : c.Name
            }).ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 10. FULL OUTER JOIN
    // LINQ mein direct nahi hai. Workaround: LEFT JOIN UNION RIGHT JOIN.
    // Dono queries ka shape (class aur properties) bilkul same hona chahiye.
    // =====================================================================
    [HttpGet("full-outer-join")]
    public async Task<IActionResult> FullOuterJoin()
    {
        var leftSide =
            from c in _context.Categories
            join p in _context.Products on c.Id equals p.CategoryId into cp
            from p in cp.DefaultIfEmpty()
            select new JoinRow
            {
                CategoryName = c.Name,
                ProductName = p == null ? null : p.Name
            };

        var rightSide =
            from p in _context.Products
            join c in _context.Categories on p.CategoryId equals c.Id into pc
            from c in pc.DefaultIfEmpty()
            select new JoinRow
            {
                CategoryName = c == null ? null : c.Name,
                ProductName = p.Name
            };

        var result = await leftSide.Union(rightSide).AsNoTracking().ToListAsync();

        return Ok(result);
    }

    // =====================================================================
    // 11. CROSS JOIN (har category x har product)
    // Rows = Categories count * Products count, isliye Take() lagaya hai.
    // Real use kam hai: combinations/matrix banane mein.
    // SQL: ... FROM Categories CROSS JOIN Products
    // =====================================================================
    [HttpGet("cross-join")]
    public async Task<IActionResult> CrossJoin()
    {
        var result = await (
            from c in _context.Categories.AsNoTracking()
            from p in _context.Products.AsNoTracking()
            select new
            {
                CategoryName = c.Name,
                ProductName = p.Name
            })
            .Take(50)
            .ToListAsync();

        return Ok(result);
    }

    // Full outer join ke liye common shape
    private sealed class JoinRow
    {
        public string? CategoryName { get; set; }
        public string? ProductName { get; set; }
    }
}