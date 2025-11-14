namespace DavidHome.RssFeed.Optimizely.Models;

public interface IFeedItemContainerIdentifiable
{
    Type? ContainerType { get; }
}