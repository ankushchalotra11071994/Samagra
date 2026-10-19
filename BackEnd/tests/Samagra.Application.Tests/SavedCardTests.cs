using Samagra.Domain.Entities;
using Shouldly;

namespace Samagra.Domain.Tests;

public class SavedCardTests
{
    private static SavedCard Card(int month, int year) => new()
    {
        Brand = "Visa",
        Last4 = "4242",
        ExpiryMonth = month,
        ExpiryYear = year
    };

    [Fact]
    public void Not_expired_until_the_month_is_over()
    {
        Card(3, 2026).HasExpired(new DateTime(2026, 3, 31)).ShouldBeFalse();
        Card(3, 2026).HasExpired(new DateTime(2026, 4, 1)).ShouldBeTrue();
    }

    [Fact]
    public void Shows_only_the_last_four_digits()
    {
        Card(12, 2030).Display().ShouldBe("Visa ···· 4242");
    }

    [Fact]
    public void Stores_no_full_card_number_field()
    {
        // Jaan boojh kar: SavedCard par PAN/CVV rakhne ki koi property nahi honi chahiye.
        var names = typeof(SavedCard).GetProperties().Select(p => p.Name).ToArray();

        names.ShouldNotContain("Number");
        names.ShouldNotContain("CardNumber");
        names.ShouldNotContain("Pan");
        names.ShouldNotContain("Cvv");
        names.ShouldNotContain("SecurityCode");
    }
}
