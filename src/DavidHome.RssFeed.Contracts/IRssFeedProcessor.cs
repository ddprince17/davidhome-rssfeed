using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedProcessor
{
    Task<bool> IsValidFeedModel(IRssFeedSourceBase? feedModel);
    Task PreProcess(IRssFeedSourceBase? feedModel);
    Task PostProcess(IRssFeedBase? feedModel, object? syndicationModel);
}