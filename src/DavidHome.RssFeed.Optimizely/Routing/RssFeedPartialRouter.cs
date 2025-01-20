using DavidHome.RssFeed.Optimizely.Models;
using DavidHome.RssFeed.Optimizely.Models.Options;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using Microsoft.Extensions.Options;

namespace DavidHome.RssFeed.Optimizely.Routing;

public class RssFeedPartialRouter<TFeedContainer> : RssFeedPartialRouter, IPartialRouter<TFeedContainer, RssFeedRoutedData> where TFeedContainer : class, IContent
{
    private const int MaxDept = 20;
    private readonly IOptionsMonitor<RssFeedOptimizelyOptions> _rssFeedOptions;
    private readonly IContentLoader _contentLoader;
    private RssFeedOptimizelyOptions DefaultOptions => _rssFeedOptions.CurrentValue;
    private RssFeedOptimizelyOptions FeedOptions => _rssFeedOptions.Get(typeof(TFeedContainer).Name);

    public RssFeedPartialRouter(IOptionsMonitor<RssFeedOptimizelyOptions> rssFeedOptions, IContentLoader contentLoader)
    {
        _rssFeedOptions = rssFeedOptions;
        _contentLoader = contentLoader;
    }

    public object RoutePartial(TFeedContainer content, UrlResolverContext segmentContext)
    {
        var feedSegments = GetFeedRelativeUrl().Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();

        return IsFeedSegment(feedSegments, segmentContext, segmentContext.GetNextSegment())
            ? new RssFeedRoutedData { FeedId = content.ContentGuid.ToString("N") }
            : new RssFeedRoutedData();
    }

    public PartialRouteData GetPartialVirtualPath(RssFeedRoutedData content, UrlGeneratorContext urlGeneratorContext)
    {
        if (Guid.TryParse(content.FeedId, out var contentGuid) && _contentLoader.TryGet(contentGuid, out TFeedContainer feedContainer))
        {
            return new PartialRouteData
            {
                BasePathRoot = feedContainer.ContentLink,
                PartialVirtualPath = GetFeedRelativeUrl()
            };
        }

        return new PartialRouteData();
    }

    private static bool IsFeedSegment(string[] feedSegments, UrlResolverContext segmentContext, Segment remainingSegment, int maxDepth = 0)
    {
        if (maxDepth >= MaxDept)
        {
            return false;
        }

        var expectedSegment = feedSegments[0];
        var nextSegment = remainingSegment.Next.ToString();

        // We are currently evaluating the first segment of the feed. We can safely remove it from the array.
        feedSegments = feedSegments.Length > 1 ? feedSegments[1..] : [];

        if (expectedSegment == nextSegment && feedSegments.Length <= 0)
        {
            // We have successfully handled the feed segment. This tells Optimizely that we no longer need to process them.
            segmentContext.RemainingSegments = remainingSegment.Remaining;

            return true;
        }

        if (expectedSegment == nextSegment && remainingSegment.Remaining.IsEmpty && feedSegments.Length > 0)
        {
            return false;
        }

        return expectedSegment == nextSegment && IsFeedSegment(feedSegments, segmentContext, segmentContext.GetNextSegment(remainingSegment.Remaining), ++maxDepth);
    }
    
    private string GetFeedRelativeUrl()
    {
        return FeedOptions.FeedRelativeUrl ?? DefaultOptions.FeedRelativeUrl ?? "rss";
    }
}

public abstract class RssFeedPartialRouter;