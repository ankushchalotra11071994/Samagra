 namespace Samagra.Domain.Entities; 
public class Buyer
{
    public Guid Id { get; private set; }
    public string BusinessName { get; private set; }
    public string PriceTier { get; private set; }
    public bool IsActive { get; private set; }

    // EF Core needs this. Private, so your code can't reach it.
    private Buyer()
    {
        BusinessName = string.Empty;
        PriceTier = string.Empty;
    }

    public Buyer(string businessName, string priceTier)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required.", nameof(businessName));
        if (string.IsNullOrWhiteSpace(priceTier))
            throw new ArgumentException("Price tier is required.", nameof(priceTier));

        Id = Guid.NewGuid();
        BusinessName = businessName.Trim();
        PriceTier = priceTier.Trim();
        IsActive = false;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}