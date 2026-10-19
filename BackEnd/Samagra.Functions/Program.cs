using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Samagra.Functions.Import;

var builder = FunctionsApplication.CreateBuilder(args);



var config = builder.Configuration;

builder.Services.AddSingleton(new ImportSettings(
    SqlConnectionString: config["SqlConnection"]
        ?? throw new InvalidOperationException("SqlConnection setting is missing."),
    ContainerName: config["BulkImportContainerName"] ?? "bulk-imports"));

builder.Services.AddSingleton(new BlobServiceClient(
    config["BlobStorage"] ?? throw new InvalidOperationException("BlobStorage setting is missing.")));

builder.Services.AddSingleton<ProductCsvImporter>();

builder.Build().Run();
