using System.Text.RegularExpressions;

namespace Samagra.AI.Rag;

// Table name config से आता है और SQL में सीधा जाता है — इसलिए सिर्फ safe identifier allow करो
internal static partial class VectorTableName
{
    public static string Validate(string tableName) =>
        IdentifierRegex().IsMatch(tableName)
            ? tableName
            : throw new InvalidOperationException($"Invalid vector table name '{tableName}'.");

    [GeneratedRegex("^[a-z_][a-z0-9_]{0,62}$")]
    private static partial Regex IdentifierRegex();
}
