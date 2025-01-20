using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedDiscoveryService
{
    IAsyncEnumerable<FeedDiscoveryResult> ResolveFeeds();
}