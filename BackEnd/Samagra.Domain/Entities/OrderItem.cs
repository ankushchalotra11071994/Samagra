// Samagra.Domain/Entities/OrderItem.cs

namespace Samagra.Domain.Entities;

public class OrderItem
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     OrderId   { get; set; }
    public Guid     ProductId { get; set; }
    public int      Quantity  { get; set; }
    public decimal  UnitPrice { get; set; }

    // Navigation Properties
    public Order    Order     { get; set; } = null!;
    public Product  Product   { get; set; } = null!;
}