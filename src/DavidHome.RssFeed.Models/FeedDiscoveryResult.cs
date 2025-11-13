namespace DavidHome.RssFeed.Models;

public class FeedDiscoveryResult
{
    public string? HostNameIdentifier { get; set; }
    public string? Language { get; set; }
    public IRssFeedSourceContainer? FeedContainer { get; set; }
    public IEnumerable<IRssFeedSourceItem?>? FeedItems { get; set; }
}