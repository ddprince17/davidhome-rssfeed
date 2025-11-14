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
        TransformSource(ref feedModel);

        var feedContainer = feedModel as IRssFeedContainer;
        var content = (feedContainer as IContentRssFeed)?.Content;

        ProcessCommonOptimizelyProperties(feedContainer);

        feedContainer?.RssInternalId = content?.ContentGuid.ToString("N");

        return Task.CompletedTask;
    }

    public Task PostProcess(IRssFeedBase? feedModel, object? syndicationModel)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global -> Intended. It is enforced during initialization.
        if (feedModel is not IContentRssFeed { Content: { } content } || syndicationModel is not SyndicationFeed syndicationFeed)
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

    public void TransformSource(ref IRssFeedSourceBase? feedModel)
    {
        feedModel = feedModel?.TransformToFeedContainer();
    }
}