using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models;
using DavidHome.RssFeed.Optimizely.Models;
using DavidHome.RssFeed.Optimizely.Models.Extensions;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace DavidHome.RssFeed.Optimizely.Services;

public class OptimizelyContentContainerProcessor : OptimizelyProcessorBase, IRssFeedContainerProcessor
{
    private readonly IUrlResolver _urlResolver;

    public OptimizelyContentContainerProcessor(IUrlResolver urlResolver) : base(urlResolver)
    {
        _urlResolver = urlResolver;
    }

    public Task<bool> IsValidFeedModel(IRssFeedSourceBase? feedModel)
    {
        return Task.FromResult(true);
    }

    public Task PreProcess(IRssFeedSourceBase? feedModel)
    {
        IRssFeedContainer rssFeedContainer;
        IContent? content;

        switch (feedModel)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            case IContent sourceContent:
                rssFeedContainer = feedModel.CreateOrGetFeedContainer();
                content = sourceContent;
                break;
            case IRssFeedContainer existingRssFeedItem and IContentRssFeed contentRssFeed:
                rssFeedContainer = existingRssFeedItem;
                content = contentRssFeed.Content;
                break;
            default:
                return Task.CompletedTask;
        }

        ProcessCommonOptimizelyProperties(rssFeedContainer);

        rssFeedContainer.RssInternalId = content?.ContentGuid.ToString("N");

        return Task.CompletedTask;
    }

    public Task PostProcess(IRssFeedBase? feedModel, object? syndicationModel)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global -> Intended. It is enforced during initialization.
        if (feedModel is not IContent content || syndicationModel is not SyndicationFeed syndicationFeed)
        {
            return Task.CompletedTask;
        }

        var rssFeedBaseUrl = _urlResolver.GetPartialRoutedUrl(new RssFeedRoutedData { FeedId = content.ContentGuid.ToString("N") });

        if (Uri.TryCreate(rssFeedBaseUrl, UriKind.Absolute, out var feedUri))
        {
            syndicationFeed.BaseUri = feedUri;
        }

        return Task.CompletedTask;
    }
}