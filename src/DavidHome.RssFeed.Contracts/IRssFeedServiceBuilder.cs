using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.RssFeed.Contracts;

public interface IRssFeedServiceBuilder
{
    IServiceCollection Services { get; }
}