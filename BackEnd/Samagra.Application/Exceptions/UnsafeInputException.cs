
namespace Samagra.Application.Exceptions;

public sealed class UnsafeInputException : Exception
{
    public string Pattern { get; }

    public UnsafeInputException(string pattern)
        : base("That request can't be processed. Please rephrase your question.")
    {
        Pattern = pattern;
    }
}