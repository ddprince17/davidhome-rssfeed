using System.Reflection;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models;
using DavidHome.RssFeed.Optimizely.Contracts;
using DavidHome.RssFeed.Optimizely.Models;
using DavidHome.RssFeed.Optimizely.Models.Options;
using DavidHome.RssFeed.Optimizely.Routing;
using DavidHome.RssFeed.Optimizely.Services;
using EPiServer.Core;
using EPiServer.Core.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class OptimizelyRssFeedServiceBuilderExtensions
{
    public static IRssFeedServiceBuilder AddContentPageFeed<TFeedContainer, TFeedItem>(this IRssFeedServiceBuilder serviceBuilder, IConfiguration configuration,
        params IReadOnlyCollection<Assembly> assembliesToScan)
        where TFeedContainer : class, IRssFeedSourceContainer<TFeedItem>, IContent, IContentRssFeed
        where TFeedItem : IRssFeedSourceItem<TFeedContainer>, IContent, IContentRssFeed
    {
        var genericType = typeof(TFeedContainer);
        var genericTypeName = genericType.Name;
        var rssFeedConfiguration = configuration
            .GetSection(nameof(DavidHome))
            .GetSection(nameof(DavidHome.RssFeed))
            .GetSection(genericTypeName);

        serviceBuilder.Services.Configure<RssFeedOptimizelyOptions>(genericTypeName, rssFeedConfiguration);

        // This service only needs to be added once.
        serviceBuilder.Services.TryAddTransient(typeof(IRssFeedDiscoveryService), typeof(OptimizelyRssFeedDiscoveryService));

        foreach (var assembly in assembliesToScan)
        {
            var assemblyTypes = assembly.GetTypes();

            serviceBuilder.Services.RegisterAllOfDefined(assemblyTypes, typeof(IRssFeedContainerProcessor<TFeedContainer>), typeof(IRssFeedContainerProcessor));
            serviceBuilder.Services.RegisterAllOfDefined(assemblyTypes, typeof(IRssFeedItemProcessor<TFeedItem>), typeof(IRssFeedItemProcessor));
        }

        var partialRouterType = typeof(RssFeedPartialRouter<TFeedContainer>);

        serviceBuilder.Services.AddSingleton(partialRouterType);
        serviceBuilder.Services.AddSingleton(typeof(RssFeedPartialRouter), provider => provider.GetRequiredService(partialRouterType));
        serviceBuilder.Services.AddSingleton(typeof(IPartialRouter), provider => provider.GetRequiredService(partialRouterType));

        return serviceBuilder;
    }

    /// <summary>
    /// Adds the OOB provided Optimizely processors for the RSS feed service.
    /// </summary>
    /// <remarks>Please call first before <see cref="AddContentPageFeed{TFeedContainer,TFeedItem}"/>
    /// if you do not want these processors to override values from your own processors.</remarks>
    public static IRssFeedServiceBuilder AddDefaultOptimizelyProcessors(this IRssFeedServiceBuilder serviceBuilder)
    {
        serviceBuilder.Services.AddTransient<IRssFeedContainerProcessor, OptimizelyContentContainerProcessor>();
        serviceBuilder.Services.AddTransient<IRssFeedItemProcessor, OptimizelyContentItemProcessor>();

        return serviceBuilder;
    }
    
    public static IRssFeedServiceBuilder AddOptimizelyFeedIntegration(this IRssFeedServiceBuilder serviceBuilder, IConfiguration configuration)
    {
        var rssFeedConfiguration = configuration
            .GetSection(nameof(DavidHome))
            .GetSection(nameof(DavidHome.RssFeed));

        serviceBuilder.Services
            .Configure<RssFeedOptimizelyOptions>(rssFeedConfiguration)
            .AddTransient<IOptimizelyContentAreaService, OptimizelyContentAreaService>()
            .AddTransient<IOptimizelySyndicationLinkService, OptimizelySyndicationLinkService>();

        return serviceBuilder;
    }

    private static void RegisterAllOfDefined(this IServiceCollection services, IReadOnlyCollection<Type> assemblyTypes, Type serviceType, Type baseServiceType)
    {
        var definedServices = assemblyTypes.Where(type => type.IsAssignableTo(serviceType));

        foreach (var implementationType in definedServices)
        {
            services.AddTransient(serviceType, implementationType);
            services.AddTransient(baseServiceType, provider => provider.GetRequiredService(serviceType));
        }
    }
}