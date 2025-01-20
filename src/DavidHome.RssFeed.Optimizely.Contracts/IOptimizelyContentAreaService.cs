using EPiServer.Core;

namespace DavidHome.RssFeed.Optimizely.Contracts;

public interface IOptimizelyContentAreaService
{
    string? RenderAsString(ContentArea? contentArea, IEnumerable<KeyValuePair<string, object?>>? routeValues = null);
}