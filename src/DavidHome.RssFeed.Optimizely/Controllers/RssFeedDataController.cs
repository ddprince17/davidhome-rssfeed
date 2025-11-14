using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Optimizely.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web;
using EPiServer.Web.Routing.Matching;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.RssFeed.Optimizely.Controllers;

public class RssFeedDataController : Controller, IRenderTemplate<RssFeedRoutedData>
{
    private readonly IEnumerable<IRssFeedStorageProvider> _rssFeedStorageProviders;
    private readonly IContentLoader _contentLoader;
    private readonly ISiteDefinitionResolver _siteDefinitionResolver;
    private readonly IRequestHostResolver _requestHostResolver;

    public RssFeedDataController(IEnumerable<IRssFeedStorageProvider> rssFeedStorageProviders, IContentLoader contentLoader, ISiteDefinitionResolver siteDefinitionResolver,
        IRequestHostResolver requestHostResolver)
    {
        _rssFeedStorageProviders = rssFeedStorageProviders;
        _contentLoader = contentLoader;
        _siteDefinitionResolver = siteDefinitionResolver;
        _requestHostResolver = requestHostResolver;
    }

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Features.Get<IContentRouteFeature>()?.RoutedContentData.PartialRoutedObject is not RssFeedRoutedData feedRoutedData)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(feedRoutedData.FeedId))
        {
            return NotFound();
        }

        if (!Guid.TryParse(feedRoutedData.FeedId, out var contentGuid) || !_contentLoader.TryGet(contentGuid, out IContent feedContainer))
        {
            return NotFound();
        }

        var siteDefinition = _siteDefinitionResolver.GetByContent(feedContainer.ContentLink, false);
        
        if (siteDefinition == null)
        {
            return NotFound();
        }
        
        foreach (var rssFeedStorageProvider in _rssFeedStorageProviders)
        {
            var feedSteam = await rssFeedStorageProvider.GetSavedStream(feedRoutedData.FeedId, (feedContainer as ILocale)?.Language.Name, siteDefinition.Id.ToString("N"));

            if (feedSteam is not null)
            {
                return File(feedSteam, "text/xml");
            }
        }

        return NotFound();
    }
}