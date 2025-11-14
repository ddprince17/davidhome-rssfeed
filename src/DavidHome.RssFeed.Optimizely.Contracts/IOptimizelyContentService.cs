using EPiServer.Core;

namespace DavidHome.RssFeed.Optimizely.Contracts;

public interface IOptimizelyContentService
{
    string? GetContentHtml(IContent? content, string contentPropertyName, IEnumerable<KeyValuePair<string, object?>>? routeValues = null);
}