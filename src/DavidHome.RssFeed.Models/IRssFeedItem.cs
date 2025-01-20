using System.ServiceModel.Syndication;

namespace DavidHome.RssFeed.Models;

public interface IRssFeedItem : IRssFeedBase
{
    SyndicationContent? RssContent { get; set; }
    ICollection<SyndicationCategory?>? RssCategories { get; set; }
    ICollection<SyndicationPerson?>? RssAuthors { get; set; }
}

public interface IRssFeedItem<TFeedContainer> : IRssFeedItem where TFeedContainer : IRssFeedContainer;