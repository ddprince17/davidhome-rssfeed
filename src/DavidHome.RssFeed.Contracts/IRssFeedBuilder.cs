using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedBuilder
{
    IAsyncEnumerable<SyndicationFeedResult?> BuildFeeds();
}