using System.Text.RegularExpressions;

namespace Samagra.AI.Guardrails;

internal static partial class PiiMasker
{
    public static string Mask(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var result = CardNumber().Replace(input, "[CARD]");
        result = Email().Replace(result, "[EMAIL]");
        result = IndianPhone().Replace(result, "[PHONE]");
        result = Aadhaar().Replace(result, "[AADHAAR]");

        return result;
    }

    [GeneratedRegex(@"\b(?:\d[ -]*?){13,16}\b")]
    private static partial Regex CardNumber();

    [GeneratedRegex(@"\b[\w.%+-]+@[\w.-]+\.[A-Za-z]{2,}\b")]
    private static partial Regex Email();

    [GeneratedRegex(@"\b(?:\+?91[\s-]?)?[6-9]\d{9}\b")]
    private static partial Regex IndianPhone();

    [GeneratedRegex(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}\b")]
    private static partial Regex Aadhaar();
}