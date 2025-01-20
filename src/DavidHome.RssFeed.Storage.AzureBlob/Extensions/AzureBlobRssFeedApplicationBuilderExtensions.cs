// ReSharper disable CheckNamespace

using Azure.Storage.Blobs;
using DavidHome.RssFeed.Storage.AzureBlob;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

public static class AzureBlobRssFeedApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAzureBlobRssFeed(this IApplicationBuilder app)
    {
        var clientFactory = app.ApplicationServices.GetRequiredService<IAzureClientFactory<BlobServiceClient>>();
        var blobClient = clientFactory.CreateClient(AzureBlobRssFeedServiceBuilderExtensions.BlobClientName);

        // Is making sure the container is created while starting the app.
        blobClient
            .GetBlobContainerClient(BlobRssFeedStorageProvider.ContainerName)
            .CreateIfNotExists();

        return app;
    }
}