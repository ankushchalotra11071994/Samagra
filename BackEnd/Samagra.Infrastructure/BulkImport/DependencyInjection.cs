using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Samagra.Application.Interfaces;
using Samagra.Infrastructure.Repositories;

namespace Samagra.Infrastructure.BulkImport;

public static class DependencyInjection
{
    public static IServiceCollection AddBulkImport(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<BulkImportOptions>()
            .Bind(config.GetSection(BulkImportOptions.SectionName));

        // Connection strings first use पर check होते हैं, startup पर नहीं —
        // ताकि बिना Azure setup के बाकी API local चलती रहे
        services.AddSingleton(_ => new BlobServiceClient(
            config.GetConnectionString("BlobStorage")
            ?? throw new InvalidOperationException("ConnectionStrings:BlobStorage is not configured.")));

        services.AddSingleton(_ => new ServiceBusClient(
            config.GetConnectionString("ServiceBus")
            ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is not configured.")));

        services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>()
            .CreateSender(sp.GetRequiredService<IOptions<BulkImportOptions>>().Value.QueueName));

        services.AddSingleton<IBulkImportStorage, AzureBlobBulkImportStorage>();
        services.AddSingleton<IBulkImportQueue, ServiceBusBulkImportQueue>();
        services.AddScoped<IBulkImportJobRepository, BulkImportJobRepository>();

        return services;
    }
}
