using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models;
using DavidHome.RssFeed.Optimizely.Models;
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

    public Task<bool> IsValidFeedModel(IRssFeedBase? feedModel)
    {
        return Task.FromResult(true);
    }

    public Task PreProcess(IRssFeedBase? feedModel)
    {
        ProcessCommonOptimizelyProperties(feedModel);

        // ReSharper disable once SuspiciousTypeConversion.Global - This is expected and enforced in the initialization.
        if (feedModel is not (IRssFeedContainer rssFeedContainer and IContent content))
        {
            return Task.CompletedTask;
        }

        rssFeedContainer.RssInternalId = content.ContentGuid.ToString("N");

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