namespace Samagra.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // foreign key + navigation
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
     public string? ImageUrl { get; set; } = null!;
}