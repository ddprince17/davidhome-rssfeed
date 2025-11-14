using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models;
using Microsoft.Extensions.Logging;

namespace DavidHome.RssFeed.Services;

public class RssFeedBuilder : IRssFeedBuilder
{
    private readonly IEnumerable<IRssFeedDiscoveryService> _rssFeedDiscoveryServices;
    private readonly IEnumerable<IRssFeedItemProcessor> _rssFeedItemProcessors;
    private readonly IEnumerable<IRssFeedContainerProcessor> _rssFeedContainerProcessors;
    private readonly ILogger<RssFeedBuilder> _logger;

    public RssFeedBuilder(IEnumerable<IRssFeedDiscoveryService> rssFeedDiscoveryServices, IEnumerable<IRssFeedItemProcessor> rssFeedItemProcessors,
        IEnumerable<IRssFeedContainerProcessor> rssFeedContainerProcessors, ILogger<RssFeedBuilder> logger)
    {
        _rssFeedDiscoveryServices = rssFeedDiscoveryServices;
        _rssFeedItemProcessors = rssFeedItemProcessors;
        _rssFeedContainerProcessors = rssFeedContainerProcessors;
        _logger = logger;
    }

    public async IAsyncEnumerable<SyndicationFeedResult?> BuildFeeds()
    {
        // ReSharper disable once LoopCanBeConvertedToQuery - Readability
        foreach (var rssFeedDiscoveryService in _rssFeedDiscoveryServices)
        {
            await foreach (var feedDiscoveryResult in rssFeedDiscoveryService.ResolveFeeds())
            {
                if (feedDiscoveryResult is not { FeedContainer: not null, FeedItems: not null })
                {
                    continue;
                }

                var feedItems = _rssFeedItemProcessors
                    .Aggregate(
                        feedDiscoveryResult.FeedItems!.Where(item => item != null),
                        (items, processor) => processor.ManipulateEnumerable(items))
                    .Select(async item => await CreateSyndicationItem(item));
                
                yield return await CreateSyndicationFeed(feedDiscoveryResult, feedItems);
            }
        }
    }

    private async Task<SyndicationFeedResult?> CreateSyndicationFeed(FeedDiscoveryResult? feedDiscoveryResult, IEnumerable<Task<SyndicationItem?>> feedItemsTask)
    {
        var feedContainerSource = feedDiscoveryResult?.FeedContainer;
        
        await PreProcessContainer(feedContainerSource);
        
        // ReSharper disable once SuspiciousTypeConversion.Global - Intended.
        if (feedContainerSource is not IRssFeedContainer feedContainer)
        {
            _logger.LogWarning(
                "The feed container source has not been converted to an implementation of '{feedContainerName}' during pre-processing. Either use the same object as source and destination or create an implementation of '{processorName}'.",
                nameof(IRssFeedContainer), nameof(IRssFeedContainerProcessor));
            
            return null;
        }

        var feedItems = await Task.WhenAll(feedItemsTask);
        var lastUpdatedTime = feedItems.OrderByDescending(item => item?.LastUpdatedTime).FirstOrDefault(item => item != null)?.LastUpdatedTime;
        var syndicationFeed = new SyndicationFeed(feedContainer.RssTitle, feedContainer.RssDescription, feedContainer.RssAlternateLink, feedContainer.RssId,
            lastUpdatedTime ?? feedContainer.RssLastUpdatedTime ?? DateTimeOffset.Now, feedItems.Where(item => item != null));

        await PostProcessContainer(feedContainer, syndicationFeed);

        return new SyndicationFeedResult
        {
            Id = feedContainer.RssInternalId,
            Feed = syndicationFeed,
            Language = feedDiscoveryResult?.Language,
            HostNameIdentifier = feedDiscoveryResult?.HostNameIdentifier
        };
    }


    private async Task<SyndicationItem?> CreateSyndicationItem(IRssFeedSourceItem? itemSource)
    {
        await PreProcessItem(itemSource);

        // ReSharper disable once SuspiciousTypeConversion.Global - Intended.
        if (itemSource is not IRssFeedItem item)
        {
            _logger.LogWarning(
                "The item source has not been converted to an implementation of '{feedItemName}' during pre-processing. Either use the same object as source and destination or create an implementation of '{processorName}'.",
                nameof(IRssFeedItem), nameof(IRssFeedItemProcessor));
            
            return null;
        }

        var syndicationItem = new SyndicationItem(item.RssTitle, item.RssContent, item.RssAlternateLink, item.RssId, item.RssLastUpdatedTime ?? DateTimeOffset.Now);

        foreach (var category in item.RssCategories?.Where(category => category != null) ?? [])
        {
            syndicationItem.Categories.Add(category);
        }

        await PostProcessItem(item, syndicationItem);

        return syndicationItem;
    }

    private async Task PostProcessContainer(IRssFeedContainer feedContainer, SyndicationFeed syndicationFeed)
    {
        foreach (var rssFeedContainerProcessor in _rssFeedContainerProcessors)
        {
            if (await rssFeedContainerProcessor.IsValidFeedModel(feedContainer))
            {
                await rssFeedContainerProcessor.PostProcess(feedContainer, syndicationFeed);
            }
        }
    }

    private async Task PreProcessContainer(IRssFeedSourceContainer? feedContainer)
    {
        foreach (var rssFeedContainerProcessor in _rssFeedContainerProcessors)
        {
            if (await rssFeedContainerProcessor.IsValidFeedModel(feedContainer))
            {
                await rssFeedContainerProcessor.PreProcess(feedContainer);
            }
        }
    }

    private async Task PostProcessItem(IRssFeedItem item, SyndicationItem syndicationItem)
    {
        foreach (var rssFeedItemProcessor in _rssFeedItemProcessors)
        {
            if (await rssFeedItemProcessor.IsValidFeedModel(item))
            {
                await rssFeedItemProcessor.PostProcess(item, syndicationItem);
            }
        }
    }

    private async Task PreProcessItem(IRssFeedSourceItem? item)
    {
        foreach (var rssFeedItemProcessor in _rssFeedItemProcessors)
        {
            if (await rssFeedItemProcessor.IsValidFeedModel(item))
            {
                await rssFeedItemProcessor.PreProcess(item);
            }
        }
    }
}