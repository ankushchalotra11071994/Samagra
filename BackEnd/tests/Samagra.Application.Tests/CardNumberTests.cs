using Shouldly;

namespace Samagra.Domain.Tests;

public class CardNumberTests
{
    // Standard test numbers — valid Luhn, not real cards
    [Theory]
    [InlineData("4242424242424242")]      // Visa
    [InlineData("4242 4242 4242 4242")]   // spaces, as typed
    [InlineData("4242-4242-4242-4242")]   // dashes
    [InlineData("5555555555554444")]      // Mastercard
    [InlineData("378282246310005")]       // Amex, 15 digits
    public void Accepts_numbers_that_pass_luhn(string number)
    {
        CardNumber.IsValid(number).ShouldBeTrue();
    }

    [Theory]
    [InlineData("4242424242424241")]      // last digit wrong
    [InlineData("1234567812345678")]      // fails checksum
    [InlineData("4242")]                  // too short
    [InlineData("42424242424242424242")]  // too long
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not a card")]
    public void Rejects_anything_that_fails_luhn(string? number)
    {
        CardNumber.IsValid(number).ShouldBeFalse();
    }

    [Theory]
    [InlineData("4242424242424242", "Visa")]
    [InlineData("5555555555554444", "Mastercard")]
    [InlineData("2223003122003222", "Mastercard")]   // 2-series
    [InlineData("378282246310005", "Amex")]
    [InlineData("6073849700000000", "RuPay")]
    [InlineData("6011111111111117", "Discover")]   // 6011 beats RuPay's broad 60
    [InlineData("6521000000000007", "RuPay")]      // 6521 beats Discover's broad 65
    [InlineData("6500000000000002", "Discover")]
    [InlineData("9999999999999999", "Card")]         // unknown prefix
    public void Reads_the_brand_from_the_leading_digits(string number, string expected)
    {
        CardNumber.Brand(number).ShouldBe(expected);
    }

    [Fact]
    public void Keeps_only_the_last_four_digits()
    {
        CardNumber.Last4("4242 4242 4242 4242").ShouldBe("4242");
        CardNumber.Last4("378282246310005").ShouldBe("0005");
        CardNumber.Last4("12").ShouldBe("");
    }

    [Fact]
    public void Card_is_good_through_the_end_of_its_expiry_month()
    {
        var lastDayOfMarch = new DateTime(2026, 3, 31);
        var firstOfApril   = new DateTime(2026, 4, 1);

        CardNumber.IsExpiryValid(3, 2026, lastDayOfMarch).ShouldBeTrue();
        CardNumber.IsExpiryValid(3, 2026, firstOfApril).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 2030)]
    [InlineData(13, 2030)]
    [InlineData(6, 1999)]
    public void Rejects_impossible_expiry_dates(int month, int year)
    {
        CardNumber.IsExpiryValid(month, year, new DateTime(2026, 1, 1)).ShouldBeFalse();
    }
}
