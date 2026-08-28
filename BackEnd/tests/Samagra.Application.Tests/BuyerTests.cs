using Samagra.Domain.Entities;
using Shouldly;

namespace Samagra.Domain.Tests;

public class BuyerTests
{
    [Fact]                                                                            
    public void New_buyer_starts_inactive()
    {
        var buyer = new Buyer("Sharma Traders", "Tier1");

        buyer.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void New_buyer_gets_an_id()
    {
        var buyer = new Buyer("Sharma Traders", "Tier1");

        buyer.Id.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Business_name_is_required(string? name)
    {
        Should.Throw<ArgumentException>(() => new Buyer(name!, "Tier1"));
    }

    [Fact]
    public void Business_name_is_trimmed()
    {
        var buyer = new Buyer("  Sharma Traders  ", "Tier1");

        buyer.BusinessName.ShouldBe("Sharma Traders");
    }

    [Fact]
    public void Activate_makes_buyer_active()
    {
        var buyer = new Buyer("Sharma Traders", "Tier1");

        buyer.Activate();

        buyer.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_makes_buyer_inactive()
    {
        var buyer = new Buyer("Sharma Traders", "Tier1");
        buyer.Activate();

        buyer.Deactivate();

        buyer.IsActive.ShouldBeFalse();
    }
}