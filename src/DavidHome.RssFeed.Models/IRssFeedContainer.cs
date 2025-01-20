namespace DavidHome.RssFeed.Models;

public interface IRssFeedContainer : IRssFeedBase
{
    string? RssInternalId { get; set; }
    string? RssDescription { get; set; }
}

public interface IRssFeedContainer<TFeedItem> : IRssFeedContainer where TFeedItem : IRssFeedItem;