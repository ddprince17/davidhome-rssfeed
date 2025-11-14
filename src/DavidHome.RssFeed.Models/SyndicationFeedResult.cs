using System.ServiceModel.Syndication;

namespace DavidHome.RssFeed.Models;

public class SyndicationFeedResult
{
    public SyndicationFeed? Feed { get; set; }
    public string? Id { get; set; }
    public string? Language { get; set; }
    public string? HostNameIdentifier { get; set; }
}