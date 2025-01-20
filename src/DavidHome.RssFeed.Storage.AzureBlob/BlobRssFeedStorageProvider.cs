using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Azure.Storage.Blobs;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DavidHome.RssFeed.Storage.AzureBlob;

public class BlobRssFeedStorageProvider : IRssFeedStorageProvider
{
    internal const string ContainerName = "dhrssfeeds";
    private readonly IAzureClientFactory<BlobServiceClient> _blobClientFactory;
    private readonly ILogger<BlobRssFeedStorageProvider> _logger;
    private readonly IOptionsMonitor<RssFeedOptions> _feedOptions;
    private BlobServiceClient RssBlobClient => _blobClientFactory.CreateClient(AzureBlobRssFeedServiceBuilderExtensions.BlobClientName);
    private BlobContainerClient RssBlobContainer => RssBlobClient.GetBlobContainerClient(ContainerName);

    public BlobRssFeedStorageProvider(IAzureClientFactory<BlobServiceClient> blobClientFactory, ILogger<BlobRssFeedStorageProvider> logger,
        IOptionsMonitor<RssFeedOptions> feedOptions)
    {
        _blobClientFactory = blobClientFactory;
        _logger = logger;
        _feedOptions = feedOptions;
    }

    public async Task Save(SyndicationFeed? feed, string? internalId = null)
    {
        var id = internalId ?? feed?.Id;
        
        if (feed == null || string.IsNullOrEmpty(id))
        {
            _logger.LogError("Feed ID is null or empty. Feed cannot be saved under this situation since there is no unique identifier.");

            return;
        }

        var blobClient = RssBlobContainer.GetBlobClient($"{id}.xml");
        var rss20Formatter = feed.GetRss20Formatter(_feedOptions.CurrentValue.SerializeExtensionsAsAtom ?? false);
        await using var blobStream = await blobClient.OpenWriteAsync(true);
        await using var xmlTextWriter = new XmlTextWriter(blobStream, Encoding.Default);
        
        rss20Formatter.WriteTo(xmlTextWriter);

        // Making sure that the content is flushed to the stream before closing it.
        await blobStream.FlushAsync();

        _logger.LogInformation("Feed with ID {FeedId} has been saved successfully.", feed.Id);
    }

    public async Task<Stream?> GetSavedStream(string id)
    {
        var blobClient = RssBlobContainer.GetBlobClient($"{id}.xml");

        return await blobClient.ExistsAsync() ? await blobClient.OpenReadAsync() : null;
    }
}