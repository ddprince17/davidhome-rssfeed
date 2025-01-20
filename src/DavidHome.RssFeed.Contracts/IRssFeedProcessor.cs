using DavidHome.RssFeed.Models;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedProcessor
{
    Task<bool> IsValidFeedModel(IRssFeedBase? feedModel);
    Task PreProcess(IRssFeedBase? feedModel);
    Task PostProcess(IRssFeedBase? feedModel, object? syndicationModel);
}