// Samagra.Domain/CardNumber.cs

namespace Samagra.Domain;

/// <summary>
/// Pure card-number checks used while saving a card. The number is inspected
/// and then thrown away — only the brand and the last four digits are kept.
/// No dependencies, so this is the one piece of the feature the existing
/// test project can cover.
/// </summary>
public static class CardNumber
{
    /// <summary>Strips spaces and dashes people type in.</summary>
    public static string Normalize(string? input) =>
        new string((input ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>Luhn checksum — catches most typos before a gateway ever sees the number.</summary>
    public static bool IsValid(string? input)
    {
        var digits = Normalize(input);
        if (digits.Length is < 12 or > 19) return false;

        var sum = 0;
        var doubling = false;

        // Right se left — har doosre digit ko double karte hain
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var value = digits[i] - '0';

            if (doubling)
            {
                value *= 2;
                if (value > 9) value -= 9;
            }

            sum += value;
            doubling = !doubling;
        }

        return sum % 10 == 0;
    }

    public static string Last4(string? input)
    {
        var digits = Normalize(input);
        return digits.Length >= 4 ? digits[^4..] : string.Empty;
    }

    /// <summary>Best-effort brand from the leading digits; "Card" when nothing matches.</summary>
    public static string Brand(string? input)
    {
        var d = Normalize(input);
        if (d.Length < 2) return "Card";

        var two = int.Parse(d[..2]);
        var three = d.Length >= 3 ? int.Parse(d[..3]) : 0;
        var four = d.Length >= 4 ? int.Parse(d[..4]) : 0;

        // Tarteeb maayne rakhti hai: 6011 (Discover) aur 6521/6522 (RuPay) dono
        // RuPay ke chaude "60"/"65" range ke andar aate hain, isliye pehle
        // chaar-digit prefix dekhte hain, phir do-digit.
        if (two is 34 or 37) return "Amex";
        if (d[0] == '4') return "Visa";
        if (two is >= 51 and <= 55 || four is >= 2221 and <= 2720) return "Mastercard";

        if (four == 6011) return "Discover";
        if (four is 6521 or 6522) return "RuPay";

        if (two is 60 or 81 or 82) return "RuPay";
        if (two == 65 || three is >= 644 and <= 649) return "Discover";

        return "Card";
    }

    /// <summary>True when the month/year pair is a real month that has not passed.</summary>
    public static bool IsExpiryValid(int month, int year, DateTime utcNow)
    {
        if (month is < 1 or > 12) return false;
        if (year is < 2000 or > 2100) return false;

        // Card apne expiry month ke aakhri din tak chalta hai
        return new DateTime(year, month, 1).AddMonths(1) > utcNow.Date;
    }
}
