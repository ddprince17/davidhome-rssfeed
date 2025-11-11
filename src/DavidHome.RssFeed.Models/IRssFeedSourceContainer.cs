namespace DavidHome.RssFeed.Models;

public interface IRssFeedSourceContainer;

public interface IRssFeedSourceContainer<TFeedSourceItem> : IRssFeedSourceContainer where TFeedSourceItem : IRssFeedSourceItem;