using DavidHome.RssFeed.Optimizely.Contracts;
using EPiServer.Core;

namespace DavidHome.RssFeed.Optimizely.Services;

public class OptimizelyContentService : IOptimizelyContentService
{
    private readonly IContentLanguageAccessor _contentLanguageAccessor;
    private readonly IOptimizelyContentAreaService _optimizelyContentAreaService;

    public OptimizelyContentService(IContentLanguageAccessor contentLanguageAccessor, IOptimizelyContentAreaService optimizelyContentAreaService)
    {
        _contentLanguageAccessor = contentLanguageAccessor;
        _optimizelyContentAreaService = optimizelyContentAreaService;
    }

    public string? GetContentHtml(IContent? content, string contentPropertyName, IEnumerable<KeyValuePair<string, object?>>? routeValues = null)
    {
        var contentLanguage = content is ILocale locale ? locale.Language : null;
        var originalLanguage = _contentLanguageAccessor.Language;

        if (contentLanguage != null)
        {
            _contentLanguageAccessor.Language = contentLanguage;
        }

        var contentArea = content?.Property[contentPropertyName]?.Value as ContentArea;
        var contentHtml = routeValues == null ? _optimizelyContentAreaService.RenderAsString(contentArea) : _optimizelyContentAreaService.RenderAsString(contentArea, routeValues);

        _contentLanguageAccessor.Language = originalLanguage;

        return contentHtml;
    }
}