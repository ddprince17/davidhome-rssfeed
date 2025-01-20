using System.ServiceModel.Syndication;

namespace DavidHome.RssFeed.Contracts;

/// <summary>
/// Provides a contract for a storage provider that will be responsible for storing the generated RSS feed.
/// </summary>
public interface IRssFeedStorageProvider
{
    Task Save(SyndicationFeed? feed, string? internalId = null);
    Task<Stream?> GetSavedStream(string id);
}