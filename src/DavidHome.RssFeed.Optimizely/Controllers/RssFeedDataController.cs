using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Optimizely.Models;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.Web;
using EPiServer.Web.Routing.Matching;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable DuplicatedSequentialIfBodies

namespace DavidHome.RssFeed.Optimizely.Controllers;

public class RssFeedDataController : Controller, IRenderTemplate<RssFeedRoutedData>
{
    private readonly IEnumerable<IRssFeedStorageProvider> _rssFeedStorageProviders;
    private readonly IContentLoader _contentLoader;
    private readonly IApplicationResolver _applicationResolver;

    public RssFeedDataController(IEnumerable<IRssFeedStorageProvider> rssFeedStorageProviders, IContentLoader contentLoader, IApplicationResolver applicationResolver)
    {
        _rssFeedStorageProviders = rssFeedStorageProviders;
        _contentLoader = contentLoader;
        _applicationResolver = applicationResolver;
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

        var application = await _applicationResolver.GetByContentAsync(feedContainer.ContentLink, false);

        if (application == null)
        {
            return NotFound();
        }

        foreach (var rssFeedStorageProvider in _rssFeedStorageProviders)
        {
            var feedSteam = await rssFeedStorageProvider.GetSavedStream(feedRoutedData.FeedId, (feedContainer as ILocale)?.Language.Name, application.Name);

            if (feedSteam is not null)
            {
                return File(feedSteam, "text/xml");
            }
        }

        return NotFound();
    }
}