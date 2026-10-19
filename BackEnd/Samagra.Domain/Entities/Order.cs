// Samagra.Domain/Entities/Order.cs

namespace Samagra.Domain.Entities;

public class Order
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public string   UserId      { get; set; } = string.Empty;
    public decimal  TotalAmount { get; set; }
    public string   Status      { get; set; } = "PENDING";
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // Shipping snapshot — order place hone ke waqt ka pata.
    // Address row baad mein badle ya delete ho, purana order wahi dikhata hai.
    // Nullable hai kyunki in columns se pehle ke orders mein pata nahi tha.
    public Guid?    ShippingAddressId { get; set; }
    public string?  ShipToName        { get; set; }
    public string?  ShipToPhone       { get; set; }
    public string?  ShipToLine1       { get; set; }
    public string?  ShipToLine2       { get; set; }
    public string?  ShipToCity        { get; set; }
    public string?  ShipToState       { get; set; }
    public string?  ShipToPostalCode  { get; set; }
    public string?  ShipToCountry     { get; set; }

    // Navigation Property
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment? Payment     { get; set; }
}