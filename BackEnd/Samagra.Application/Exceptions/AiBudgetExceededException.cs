namespace Samagra.Application.Exceptions;

public sealed class AiBudgetExceededException : Exception
{
    public decimal SpentUsd { get; }
    public decimal LimitUsd { get; }

    public AiBudgetExceededException(decimal spentUsd, decimal limitUsd)
        : base("Daily AI usage limit reached. Try again tomorrow.")
    {
        SpentUsd = spentUsd;
        LimitUsd = limitUsd;
    }
}