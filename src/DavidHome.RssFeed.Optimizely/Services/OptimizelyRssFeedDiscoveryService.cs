using System.Globalization;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models;
using DavidHome.RssFeed.Optimizely.Models.Options;
using DavidHome.RssFeed.Optimizely.Routing;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DavidHome.RssFeed.Optimizely.Services;

public class OptimizelyRssFeedDiscoveryService : IRssFeedDiscoveryService
{
    private readonly FilterSort _sortPublishedDescending = new(FilterSortOrder.PublishedDescending);
    
    private readonly IEnumerable<RssFeedPartialRouter> _routers;
    private readonly ILogger<OptimizelyRssFeedDiscoveryService> _logger;
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentModelUsage _contentModelUsage;
    private readonly IContentLoader _contentLoader;
    private readonly IOptionsMonitor<RssFeedOptimizelyOptions> _rssFeedOptions;
    private readonly ILanguageBranchRepository _languageBranchRepository;
    private readonly IPageCriteriaQueryable _pageCriteriaQueryable;
    
    private RssFeedOptimizelyOptions DefaultOptions => _rssFeedOptions.CurrentValue;

    public OptimizelyRssFeedDiscoveryService(IEnumerable<RssFeedPartialRouter> routers, ILogger<OptimizelyRssFeedDiscoveryService> logger,
        IContentTypeRepository contentTypeRepository, IContentModelUsage contentModelUsage, IContentLoader contentLoader, IOptionsMonitor<RssFeedOptimizelyOptions> rssFeedOptions,
        ILanguageBranchRepository languageBranchRepository, IPageCriteriaQueryable pageCriteriaQueryable)
    {
        _routers = routers;
        _logger = logger;
        _contentTypeRepository = contentTypeRepository;
        _contentModelUsage = contentModelUsage;
        _contentLoader = contentLoader;
        _rssFeedOptions = rssFeedOptions;
        _languageBranchRepository = languageBranchRepository;
        _pageCriteriaQueryable = pageCriteriaQueryable;
    }

    public virtual async IAsyncEnumerable<FeedDiscoveryResult> ResolveFeeds()
    {
        foreach (var router in _routers)
        {
            var routerType = router.GetType();
            var feedContainerType = GetFeedContainerType(routerType);
            var feedItemType = GetFeedItemType(feedContainerType, routerType);

            if (feedContainerType is null || feedItemType is null)
            {
                continue;
            }

            _logger.LogInformation("Router '{routerType}' has feed container type '{feedContainerType}' and feed item type '{feedItemType}'.", routerType, feedContainerType,
                feedItemType);

            var loaderOptions = GetLoaderOptions();
            var feedContainerContentType = _contentTypeRepository.Load(feedContainerType);
            var feedItemContentType = _contentTypeRepository.Load(feedItemType);
            
            var feedContainerUsagesChunks = _contentModelUsage
                .ListContentOfContentType(feedContainerContentType)
                .Where(usage => usage != null)
                .Select(usage => new ContentReference(usage.ContentLink.ID, 0))
                .Chunk(_rssFeedOptions.CurrentValue.ContainerChunkSize ?? 100);

            foreach (var feedContainerUsages in feedContainerUsagesChunks)
            {
                var feedContainerPages = _contentLoader
                    .GetItems(feedContainerUsages, [LanguageLoaderOption.MasterLanguage()])
                    .Where(content => content != null && content.GetType().IsAssignableTo(feedContainerType) && PublishedStateAssessor.IsPublished(content));
                    // .GroupBy(content => content is ILocale localeContent ? localeContent.Language.Name : CultureInfo.InvariantCulture.Name);

                    
                    var pageTypeCriteria = new PropertyCriteria
                    {
                        Name = nameof(PropertyPageType.PageTypeID), Type = PropertyDataType.PageType, Required = true, Condition = CompareCondition.Equal,
                        Value = feedItemContentType.ID.ToString()
                    };
                    var result = _pageCriteriaQueryable.FindPagesWithCriteria(feedContainerUsages.First(), [pageTypeCriteria], "", new LanguageSelector(""));
                    _sortPublishedDescending.Filter(result);
                
                foreach (var feedContainerPage in feedContainerPages)
                {
                    
                    var feedOptions = _rssFeedOptions.Get(feedContainerType.Name);
                    var feedContainerDescendents = feedContainerPage != null ? _contentLoader.GetDescendents(feedContainerPage.ContentLink) : [];
                    var containerFeedItemPages = _contentLoader.GetItems(feedContainerDescendents, loaderOptions)
                        .Where(content => content != null && content.GetType().IsAssignableTo(feedItemType) && PublishedStateAssessor.IsPublished(content))
                        .OrderByDescending(content => content is IChangeTrackable changeTrackable ? changeTrackable.Created : DateTime.MinValue)
                        .Take(feedOptions.MaxSyndicationItems ?? DefaultOptions.MaxSyndicationItems ?? 50)
                        .Select(content => new { Language = content is ILocale localeContent ? localeContent.Language.Name : CultureInfo.InvariantCulture.Name, Page = content });

                    // ReSharper disable SuspiciousTypeConversion.Global -> We enforce this rule under the initialization logic. We expect these types.
                    yield return new FeedDiscoveryResult
                        { FeedContainer = feedContainerPage as IRssFeedSourceContainer, FeedItems = containerFeedItemPages.OfType<IRssFeedSourceItem>() };
                    // ReSharper restore SuspiciousTypeConversion.Global    
                    
                }
            }
        }

        // Will force this method to be async enumerable.
        await Task.CompletedTask;
    }

    private LoaderOptions GetLoaderOptions()
    {
        var languageLoaderOptions = _languageBranchRepository
            .ListEnabled()
            .Select(branch => LanguageLoaderOption.Specific(branch.Culture))
            .Concat([LanguageLoaderOption.MasterLanguage()])
            .ToArray();
        LoaderOptions loaderOptions = [..languageLoaderOptions];

        return loaderOptions;
    }

    private Type? GetFeedItemType(Type? feedContainerType, Type routerType)
    {
        var inheritedContainerInterface = feedContainerType?.GetInterfaces().FirstOrDefault(type => type.IsAssignableTo(typeof(IRssFeedContainer)));

        if (inheritedContainerInterface is { IsConstructedGenericType: true })
        {
            return inheritedContainerInterface.GenericTypeArguments.First();
        }

        _logger.LogError("The feed container type '{type}' from router type '{routerType}' doesn't have a feed item type defined in its generic arguments.", feedContainerType,
            routerType);

        return null;
    }

    private Type? GetFeedContainerType(Type routerType)
    {
        if (routerType.IsConstructedGenericType)
        {
            return routerType.GenericTypeArguments.First();
        }

        _logger.LogError("The router type '{type}' doesn't have a defined feed container type in its generic arguments.", routerType);

        return null;
    }
}