using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Optimizely.Models;
using EPiServer.Web;
using EPiServer.Web.Routing.Matching;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.RssFeed.Optimizely.Controllers;

public class RssFeedDataController : Controller, IRenderTemplate<RssFeedRoutedData>
{
    private readonly IEnumerable<IRssFeedStorageProvider> _rssFeedStorageProviders;

    public RssFeedDataController(IEnumerable<IRssFeedStorageProvider> rssFeedStorageProviders)
    {
        _rssFeedStorageProviders = rssFeedStorageProviders;
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

        foreach (var rssFeedStorageProvider in _rssFeedStorageProviders)
        {
            var feedSteam = await rssFeedStorageProvider.GetSavedStream(feedRoutedData.FeedId);

            if (feedSteam is not null)
            {
                return File(feedSteam, "text/xml");
            }
        }

        return NotFound();
    }
}