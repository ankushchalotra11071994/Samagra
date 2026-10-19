// Samagra.Domain/Entities/Address.cs

namespace Samagra.Domain.Entities;

/// <summary>
/// A buyer's saved shipping address. Orders keep their own snapshot of the
/// address fields, so editing or deleting this row never rewrites history.
/// </summary>
public class Address
{
    public Guid     Id         { get; set; } = Guid.NewGuid();

    // Order.UserId jaisa hi — token se aata hai, request body se kabhi nahi
    public string   UserId     { get; set; } = string.Empty;

    public string   Label      { get; set; } = string.Empty;   // "Home", "Warehouse"
    public string   FullName   { get; set; } = string.Empty;
    public string   Phone      { get; set; } = string.Empty;
    public string   Line1      { get; set; } = string.Empty;
    public string?  Line2      { get; set; }
    public string   City       { get; set; } = string.Empty;
    public string   State      { get; set; } = string.Empty;
    public string   PostalCode { get; set; } = string.Empty;
    public string   Country    { get; set; } = "India";

    public bool     IsDefault  { get; set; }

    // Soft delete — purane orders ka snapshot rehta hai, row sirf chhup jaati hai
    public bool     IsActive   { get; set; } = true;

    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    public string OneLine() =>
        string.Join(", ", new[] { Line1, Line2, City, State, PostalCode, Country }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
}
