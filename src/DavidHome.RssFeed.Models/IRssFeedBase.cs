namespace DavidHome.RssFeed.Models;

public interface IRssFeedBase
{
    string? RssId { get; set; }
    string? RssTitle { get; set; }
    Uri? RssAlternateLink { get; set; }
    DateTimeOffset? RssLastUpdatedTime { get; set; }
}