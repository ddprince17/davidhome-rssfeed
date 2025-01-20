using DavidHome.RssFeed;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models.Options;
using DavidHome.RssFeed.Services;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class RssFeedServiceCollectionExtensions
{
    public static IRssFeedServiceBuilder AddDavidHomeRssFeed(this IServiceCollection services, IConfiguration configuration)
    {
        var rssFeedConfiguration = configuration
            .GetSection(nameof(DavidHome))
            .GetSection(nameof(DavidHome.RssFeed));

        services.Configure<RssFeedOptions>(rssFeedConfiguration)
            .AddTransient<IRssFeedBuilder, RssFeedBuilder>();

        return new RssFeedServiceBuilder(services);
    }
}