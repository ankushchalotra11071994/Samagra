using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Samagra.Application.Interfaces;

namespace Samagra.Infrastructure.BulkImport;

public sealed class ServiceBusBulkImportQueue : IBulkImportQueue
{
    private readonly ServiceBusSender _sender;

    public ServiceBusBulkImportQueue(ServiceBusSender sender) => _sender = sender;

    public Task EnqueueAsync(BulkImportMessage message, CancellationToken ct = default)
    {
        var sbMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
        {
            // queue पर duplicate detection on हो तो एक job दो बार queue नहीं होगा
            MessageId = message.JobId.ToString(),
            ContentType = "application/json",
            Subject = "bulk-import"
        };

        return _sender.SendMessageAsync(sbMessage, ct);
    }
}
