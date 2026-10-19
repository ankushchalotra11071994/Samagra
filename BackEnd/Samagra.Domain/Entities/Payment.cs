// Samagra.Domain/Entities/Payment.cs

namespace Samagra.Domain.Entities;

public class Payment
{
    public Guid     Id            { get; set; } = Guid.NewGuid();
    public Guid     OrderId       { get; set; }
    public decimal  Amount        { get; set; }
    public string   Status        { get; set; } = "PENDING";
    public string   PaymentMethod { get; set; } = "UPI";

    // Card se pay kiya ho to sirf aakhri 4 digits — poora number kahin store nahi hota
    public string?  CardLast4     { get; set; }
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Order    Order         { get; set; } = null!;
}