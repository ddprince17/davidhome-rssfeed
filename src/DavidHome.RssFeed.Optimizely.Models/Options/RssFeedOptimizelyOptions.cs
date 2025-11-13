using DavidHome.RssFeed.Models.Options;

namespace DavidHome.RssFeed.Optimizely.Models.Options;

public record RssFeedOptimizelyOptions : RssFeedOptions
{
    public string? FeedRelativeUrl { get; set; } = "rss";
    public string? ContentAreaPropertyName { get; set; }
    public string? FeedTitlePropertyName { get; set; }
}