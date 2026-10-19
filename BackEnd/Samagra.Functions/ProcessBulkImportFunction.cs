using Microsoft.Azure.Functions.Worker;
using Samagra.Application.Interfaces;
using Samagra.Functions.Import;

namespace Samagra.Functions;

public sealed class ProcessBulkImportFunction
{
    private readonly ProductCsvImporter _importer;

    public ProcessBulkImportFunction(ProductCsvImporter importer) => _importer = importer;

    // Message API के BulkImportsController.Start से आता है
    [Function("ProcessBulkImport")]
    public Task Run(
        [ServiceBusTrigger("%BulkImportQueueName%", Connection = "ServiceBus")] BulkImportMessage message,
        CancellationToken ct)
        => _importer.ImportAsync(message, ct);
}
