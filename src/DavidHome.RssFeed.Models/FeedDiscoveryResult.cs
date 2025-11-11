namespace DavidHome.RssFeed.Models;

public class FeedDiscoveryResult
{
    public IRssFeedSourceContainer? FeedContainer { get; set; }
    public IEnumerable<IRssFeedSourceItem?>? FeedItems { get; set; }
}