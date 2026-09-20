// Samagra.Domain/Entities/Order.cs

namespace Samagra.Domain.Entities;

public class Order
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public string   UserId      { get; set; } = string.Empty;
    public decimal  TotalAmount { get; set; }
    public string   Status      { get; set; } = "PENDING";
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment? Payment     { get; set; }
}