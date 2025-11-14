namespace DavidHome.RssFeed.Models;

public interface IRssFeedSourceItem : IRssFeedSourceBase;

public interface IRssFeedSourceItem<TFeedContainer> : IRssFeedSourceItem where TFeedContainer : IRssFeedSourceContainer;