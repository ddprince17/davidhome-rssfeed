using EPiServer.Core;

namespace DavidHome.RssFeed.Optimizely.Services;

internal interface IContentRssFeed
{
    IContent Content { get; }
}