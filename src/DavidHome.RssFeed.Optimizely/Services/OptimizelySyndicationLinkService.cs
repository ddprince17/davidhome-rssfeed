using DavidHome.RssFeed.Optimizely.Contracts;
using DavidHome.RssFeed.Optimizely.Models;
using DavidHome.RssFeed.Optimizely.Models.Options;
using EPiServer;
using EPiServer.Web;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Matching;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace DavidHome.RssFeed.Optimizely.Services;

public class OptimizelySyndicationLinkService : IOptimizelySyndicationLinkService
{
    private readonly IUrlResolver _urlResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<RssFeedOptimizelyOptions> _feedOptions;
    private readonly ISiteDefinitionResolver _siteDefinitionResolver;

    private RssFeedOptimizelyOptions DefaultFeedOptions => _feedOptions.CurrentValue;
    private RssFeedOptimizelyOptions ContainerFeedOptions(string? containerName) => _feedOptions.Get(containerName);

    public OptimizelySyndicationLinkService(IUrlResolver urlResolver, IHttpContextAccessor httpContextAccessor, IOptionsMonitor<RssFeedOptimizelyOptions> feedOptions,
        ISiteDefinitionResolver siteDefinitionResolver)
    {
        _urlResolver = urlResolver;
        _httpContextAccessor = httpContextAccessor;
        _feedOptions = feedOptions;
        _siteDefinitionResolver = siteDefinitionResolver;
    }

    public IHtmlContent GenerateSyndicationLink()
    {
        var contentRouteFeature = _httpContextAccessor.HttpContext?.Features.Get<IContentRouteFeature>();
        var routedContent = contentRouteFeature?.RoutedContentData?.Content;
        var feedUrl = _urlResolver.GetPartialRoutedUrl(new RssFeedRoutedData { FeedId = routedContent?.ContentGuid.ToString("N") });

        if (string.IsNullOrEmpty(feedUrl) || routedContent == null)
        {
            return HtmlString.Empty;
        }

        var feedTitlePropertyName = ContainerFeedOptions(routedContent.GetOriginalType().Name).FeedTitlePropertyName ?? DefaultFeedOptions.FeedTitlePropertyName;
        var feedTitle = string.IsNullOrEmpty(feedTitlePropertyName) ? routedContent.Name : routedContent.Property[feedTitlePropertyName]?.Value as string;
        var siteDefinition = _siteDefinitionResolver.GetByContent(routedContent.ContentLink, false);

        if (Uri.TryCreate(siteDefinition?.SiteUrl, feedUrl, out var feedUri))
        {
            return new TagBuilder("link")
                { Attributes = { { "href", feedUri.ToString() }, { "rel", "alternate" }, { "title", feedTitle }, { "type", "application/rss+xml" } } };
        }

        return HtmlString.Empty;
    }
}