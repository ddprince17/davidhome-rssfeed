using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedItemProcessor : IRssFeedProcessor
{
    IEnumerable<IRssFeedItem?> ManipulateEnumerable(IEnumerable<IRssFeedItem?> feedModels) => feedModels;
}

public interface IRssFeedItemProcessor<TFeedItem> : IRssFeedItemProcessor where TFeedItem : IRssFeedItem;