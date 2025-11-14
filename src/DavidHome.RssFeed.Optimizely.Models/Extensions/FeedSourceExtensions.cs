using System.Diagnostics.CodeAnalysis;
using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Models;
using EPiServer.Core;

namespace DavidHome.RssFeed.Optimizely.Models.Extensions;

public static class FeedSourceExtensions
{
    extension<T>(T? sourceFeedBase) where T : IRssFeedSourceBase
    {
        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        public IRssFeedItem TransformToFeedItem()
        {
            return sourceFeedBase switch
            {
                IContent sourceContent => new ContentRssFeedItem { Content = sourceContent, ContainerType = sourceFeedBase.GetFeedSourceItemContainerType() },
                IRssFeedItem existingRssFeedItem and IContentRssFeed => existingRssFeedItem,
                _ => throw new ArgumentOutOfRangeException(nameof(sourceFeedBase), sourceFeedBase, $"Could not retrieve Optimizely '{nameof(IContent)}' from the feed source item.")
            };
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        public IRssFeedContainer TransformToFeedContainer()
        {
            return sourceFeedBase switch
            {
                IContent sourceContent => new ContentRssFeedContainer(sourceContent),
                IRssFeedContainer existingRssFeedContainer and IContentRssFeed => existingRssFeedContainer,
                _ => throw new ArgumentOutOfRangeException(nameof(sourceFeedBase), sourceFeedBase, $"Could not retrieve Optimizely '{nameof(IContent)}' from the feed source container.")
            };
        }

        private Type? GetFeedSourceItemContainerType()
        {
            var feedItemType = sourceFeedBase?.GetType().GetInterfaces().FirstOrDefault(type => type.IsAssignableTo(typeof(IRssFeedSourceItem)));
            Type? containerType = null;

            if (feedItemType is { IsConstructedGenericType: true })
            {
                containerType = feedItemType.GenericTypeArguments.First();
            }

            return containerType;
        }
    }

    private class ContentRssFeedItem : IRssFeedItem, IContentRssFeed, IFeedItemContainerIdentifiable
    {
        public string? RssId { get; set; }
        public string? RssTitle { get; set; }
        public Uri? RssAlternateLink { get; set; }
        public DateTimeOffset? RssLastUpdatedTime { get; set; }
        public SyndicationContent? RssContent { get; set; }
        public ICollection<SyndicationCategory?>? RssCategories { get; set; }
        public ICollection<SyndicationPerson?>? RssAuthors { get; set; }
        public required IContent Content { get; init; }
        public Type? ContainerType { get; init; }
    }

    private class ContentRssFeedContainer : IRssFeedContainer, IContentRssFeed
    {
        public string? RssId { get; set; }
        public string? RssTitle { get; set; }
        public Uri? RssAlternateLink { get; set; }
        public DateTimeOffset? RssLastUpdatedTime { get; set; }
        public string? RssInternalId { get; set; }
        public string? RssDescription { get; set; }
        public IContent Content { get; }

        public ContentRssFeedContainer(IContent content)
        {
            Content = content;
        }
    }
}