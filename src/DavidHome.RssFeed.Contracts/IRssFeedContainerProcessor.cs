using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedContainerProcessor : IRssFeedProcessor;

public interface IRssFeedContainerProcessor<in TFeedContainer> : IRssFeedContainerProcessor where TFeedContainer : IRssFeedSourceContainer;