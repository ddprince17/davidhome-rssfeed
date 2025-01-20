using DavidHome.RssFeed.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.RssFeed;

internal class RssFeedServiceBuilder : IRssFeedServiceBuilder
{
    public IServiceCollection Services { get; }

    public RssFeedServiceBuilder(IServiceCollection services)
    {
        Services = services;
    }
}