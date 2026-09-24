using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Samagra.AI.Guardrails;
using Samagra.Application.Exceptions;

namespace Samagra.AI.Middleware;

internal sealed class InputGuardChatClient : DelegatingChatClient
{
    private readonly ILogger<InputGuardChatClient> _logger;

    public InputGuardChatClient(
        IChatClient inner,
        ILogger<InputGuardChatClient> logger) : base(inner)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var checkedMessages = new List<ChatMessage>();

        foreach (var message in messages)
        {
            // सिर्फ़ user के messages जाँचो — system prompt हमारा अपना है
            if (message.Role != ChatRole.User)
            {
                checkedMessages.Add(message);
                continue;
            }

            var text = message.Text;

            if (InjectionDetector.IsSuspicious(text, out var pattern))
            {
                _logger.LogWarning("Blocked suspicious input matching '{Pattern}'", pattern);
                throw new UnsafeInputException(pattern!);
            }

            var masked = PiiMasker.Mask(text);

            if (!ReferenceEquals(masked, text) && masked != text)
                _logger.LogInformation("PII masked in user message");

            checkedMessages.Add(new ChatMessage(ChatRole.User, masked));
        }

        return await base.GetResponseAsync(checkedMessages, options, cancellationToken);
    }
}
