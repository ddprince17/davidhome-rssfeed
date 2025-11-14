using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Models;
using EPiServer.Core;

namespace DavidHome.RssFeed.Optimizely.Models.Extensions;

public static class FeedSourceExtensions
{
    public static IRssFeedItem CreateOrGetFeedItem(this IRssFeedSourceBase sourceItem)
    {
        return sourceItem switch
        {
            // ReSharper disable once SuspiciousTypeConversion.Global - Intended.
            IContent sourceContent => new ContentRssFeedItem(sourceContent),
            IRssFeedItem existingRssFeedItem and IContentRssFeed => existingRssFeedItem,
            _ => throw new ArgumentOutOfRangeException(nameof(sourceItem), sourceItem, $"Could not retrieve Optimizely '{nameof(IContent)}' from the feed source item.")
        };
    }

    public static IRssFeedContainer CreateOrGetFeedContainer(this IRssFeedSourceBase sourceContainer)
    {
        return sourceContainer switch
        {
            // ReSharper disable once SuspiciousTypeConversion.Global - Intended.
            IContent sourceContent => new ContentRssFeedContainer(sourceContent),
            IRssFeedContainer existingRssFeedContainer and IContentRssFeed => existingRssFeedContainer,
            _ => throw new ArgumentOutOfRangeException(nameof(sourceContainer), sourceContainer, $"Could not retrieve Optimizely '{nameof(IContent)}' from the feed source container.")
        };
    }

    private class ContentRssFeedItem : IRssFeedItem, IContentRssFeed
    {
        public IContent Content { get; }
        public string? RssId { get; set; }
        public string? RssTitle { get; set; }
        public Uri? RssAlternateLink { get; set; }
        public DateTimeOffset? RssLastUpdatedTime { get; set; }
        public SyndicationContent? RssContent { get; set; }
        public ICollection<SyndicationCategory?>? RssCategories { get; set; }
        public ICollection<SyndicationPerson?>? RssAuthors { get; set; }

        public ContentRssFeedItem(IContent content)
        {
            Content = content;
        }
    }

    private class ContentRssFeedContainer : IRssFeedContainer, IContentRssFeed
    {
        public IContent Content { get; }
        public string? RssId { get; set; }
        public string? RssTitle { get; set; }
        public Uri? RssAlternateLink { get; set; }
        public DateTimeOffset? RssLastUpdatedTime { get; set; }
        public string? RssInternalId { get; set; }
        public string? RssDescription { get; set; }

        public ContentRssFeedContainer(IContent content)
        {
            Content = content;
        }
    }
}