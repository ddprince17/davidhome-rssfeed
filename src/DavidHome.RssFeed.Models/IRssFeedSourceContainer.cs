namespace DavidHome.RssFeed.Models;

public interface IRssFeedSourceContainer : IRssFeedSourceBase;

public interface IRssFeedSourceContainer<TFeedSourceItem> : IRssFeedSourceContainer where TFeedSourceItem : IRssFeedSourceItem;