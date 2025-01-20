using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Services;

public class RssFeedBuilder : IRssFeedBuilder
{
    private readonly IEnumerable<IRssFeedDiscoveryService> _rssFeedDiscoveryServices;
    private readonly IEnumerable<IRssFeedItemProcessor> _rssFeedItemProcessors;
    private readonly IEnumerable<IRssFeedContainerProcessor> _rssFeedContainerProcessors;

    public RssFeedBuilder(IEnumerable<IRssFeedDiscoveryService> rssFeedDiscoveryServices, IEnumerable<IRssFeedItemProcessor> rssFeedItemProcessors,
        IEnumerable<IRssFeedContainerProcessor> rssFeedContainerProcessors)
    {
        _rssFeedDiscoveryServices = rssFeedDiscoveryServices;
        _rssFeedItemProcessors = rssFeedItemProcessors;
        _rssFeedContainerProcessors = rssFeedContainerProcessors;
    }

    public async IAsyncEnumerable<SyndicationFeedResult> BuildFeeds()
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

                yield return await CreateSyndicationFeed(feedDiscoveryResult.FeedContainer, await Task.WhenAll(feedItems));
            }
        }
    }

    private async Task<SyndicationFeedResult> CreateSyndicationFeed(IRssFeedContainer? feedContainer, IEnumerable<SyndicationItem> feedItems)
    {
        await PreProcessContainer(feedContainer);

        var syndicationFeed = new SyndicationFeed(feedContainer!.RssTitle, feedContainer.RssDescription, feedContainer.RssAlternateLink, feedContainer.RssId,
            feedContainer.RssLastUpdatedTime ?? DateTimeOffset.Now, feedItems);

        await PostProcessContainer(feedContainer, syndicationFeed);

        return new SyndicationFeedResult { Id = feedContainer.RssInternalId, Feed = syndicationFeed };
    }


    private async Task<SyndicationItem> CreateSyndicationItem(IRssFeedItem? item)
    {
        await PreProcessItem(item);

        var syndicationItem = new SyndicationItem(item!.RssTitle, item.RssContent, item.RssAlternateLink, item.RssId, item.RssLastUpdatedTime ?? DateTimeOffset.Now);

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

    private async Task PreProcessContainer(IRssFeedContainer? feedContainer)
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

    private async Task PreProcessItem(IRssFeedItem? item)
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