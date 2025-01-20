using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Models;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace DavidHome.RssFeed.Optimizely.Services;

public abstract class OptimizelyProcessorBase
{
    private readonly IUrlResolver _urlResolver;

    public OptimizelyProcessorBase(IUrlResolver urlResolver)
    {
        _urlResolver = urlResolver;
    }

    protected void ProcessCommonOptimizelyProperties(IRssFeedBase? feedModel)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global - This is expected and enforced in the initialization.
        if (feedModel is not IContent content)
        {
            return;
        }

        var contentUrl = _urlResolver.GetUrl(content);

        // Assuming this is absolute. Technically it should if the scheduled job is not running under the http context of the user.
        if (Uri.TryCreate(contentUrl, UriKind.Absolute, out var uri))
        {
            feedModel.RssAlternateLink = uri;
            feedModel.RssId = uri.ToString();
        }

        if (string.IsNullOrEmpty(feedModel.RssId))
        {
            feedModel.RssId = content.ContentGuid.ToString("N");
        }

        feedModel.RssTitle = content.Name;
        feedModel.RssLastUpdatedTime = content is IChangeTrackable changeTrackable ? changeTrackable.Saved.ToUniversalTime() : null;
    }
}