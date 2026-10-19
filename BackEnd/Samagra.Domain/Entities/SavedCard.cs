// Samagra.Domain/Entities/SavedCard.cs

namespace Samagra.Domain.Entities;

/// <summary>
/// A card the buyer has saved for checkout.
///
/// IMPORTANT: the full card number (PAN) and the CVV are NEVER stored — not in
/// this entity, not in the database, not in logs. The API validates the number
/// the browser sends, derives <see cref="Brand"/> and <see cref="Last4"/> from
/// it, and discards the rest. That is everything the UI needs to render
/// "Visa ···· 4242", and it keeps this table out of PCI-DSS scope.
///
/// When a real gateway is wired up (Razorpay/Stripe), its card token belongs in
/// a new column here — the PAN still does not.
/// </summary>
public class SavedCard
{
    public Guid     Id             { get; set; } = Guid.NewGuid();

    public string   UserId         { get; set; } = string.Empty;

    public string   Brand          { get; set; } = string.Empty;   // Visa / Mastercard / RuPay / Amex
    public string   Last4          { get; set; } = string.Empty;   // sirf aakhri 4 digits
    public string   CardholderName { get; set; } = string.Empty;

    public int      ExpiryMonth    { get; set; }
    public int      ExpiryYear     { get; set; }

    public bool     IsDefault      { get; set; }
    public bool     IsActive       { get; set; } = true;           // soft delete

    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    /// <summary>A card is good through the last day of its expiry month.</summary>
    public bool HasExpired(DateTime utcNow) =>
        new DateTime(ExpiryYear, ExpiryMonth, 1).AddMonths(1) <= utcNow.Date;

    public string Display() => $"{Brand} ···· {Last4}";
}
