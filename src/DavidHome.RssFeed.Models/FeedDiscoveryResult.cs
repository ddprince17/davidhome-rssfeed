namespace DavidHome.RssFeed.Models;

public class FeedDiscoveryResult
{
    public IRssFeedContainer? FeedContainer { get; set; }
    public IEnumerable<IRssFeedItem?>? FeedItems { get; set; }
}