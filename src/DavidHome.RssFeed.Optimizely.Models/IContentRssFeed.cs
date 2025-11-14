using EPiServer.Core;

namespace DavidHome.RssFeed.Optimizely.Models;

public interface IContentRssFeed
{
    // ReSharper disable once SuspiciousTypeConversion.Global - Under certain circumstances, this cast is right.
    IContent? Content => this as IContent;
}