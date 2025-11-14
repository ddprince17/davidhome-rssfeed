using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedItemProcessor : IRssFeedProcessor
{
    IEnumerable<IRssFeedSourceItem?> ManipulateEnumerable(IEnumerable<IRssFeedSourceItem?> feedModels) => feedModels;
}

public interface IRssFeedItemProcessor<TFeedItem> : IRssFeedItemProcessor where TFeedItem : IRssFeedSourceItem;