using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Storage.AzureBlob;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class AzureBlobRssFeedServiceBuilderExtensions
{
    internal const string BlobClientName = "DavidHomeRssFeed";

    public static IRssFeedServiceBuilder AddAzureBlobStorage(this IRssFeedServiceBuilder serviceBuilder, IConfiguration configuration)
    {
        serviceBuilder.Services
            .AddTransient<IRssFeedStorageProvider, BlobRssFeedStorageProvider>()
            .AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(configuration).WithName(BlobClientName);
            });

        return serviceBuilder;
    }
}