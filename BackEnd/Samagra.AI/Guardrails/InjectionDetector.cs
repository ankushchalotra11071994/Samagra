namespace Samagra.AI.Guardrails;

internal static class InjectionDetector
{
    private static readonly string[] Patterns =
    [
        "ignore previous",
        "ignore all previous",
        "ignore your instructions",
        "disregard previous",
        "disregard your instructions",
        "forget your instructions",
        "forget everything",
        "you are now",
        "act as",
        "pretend to be",
        "system prompt",
        "your instructions are",
        "reveal your prompt",
        "print your prompt",
        "show me your prompt",
        "developer mode",
        "jailbreak",
        "bypass",
        "all users",
        "every user",
        "all customers",
        "other customers",
        "everyone's orders"
    ];

    public static bool IsSuspicious(string input, out string? matched)
    {
        matched = Patterns.FirstOrDefault(p =>
            input.Contains(p, StringComparison.OrdinalIgnoreCase));

        return matched is not null;
    }
}