using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Models;
using DavidHome.RssFeed.Optimizely.Models.Options;
using DavidHome.RssFeed.Optimizely.Routing;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Filters;
using EPiServer.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DavidHome.RssFeed.Optimizely.Services;

public class OptimizelyRssFeedDiscoveryService : IRssFeedDiscoveryService
{
    private readonly FilterSort _sortPublishedDescending = new(FilterSortOrder.PublishedDescending);

    private readonly IEnumerable<RssFeedPartialRouter> _routers;
    private readonly ILogger<OptimizelyRssFeedDiscoveryService> _logger;
    private readonly IOptionsMonitor<RssFeedOptimizelyOptions> _rssFeedOptions;
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly ILanguageBranchRepository _languageBranchRepository;
    private readonly IPageCriteriaQueryable _pageCriteriaQueryable;
    private readonly ISiteDefinitionResolver _siteDefinitionResolver;

    private RssFeedOptimizelyOptions DefaultOptions => _rssFeedOptions.CurrentValue;

    public OptimizelyRssFeedDiscoveryService(IEnumerable<RssFeedPartialRouter> routers, ILogger<OptimizelyRssFeedDiscoveryService> logger,
        IOptionsMonitor<RssFeedOptimizelyOptions> rssFeedOptions, IContentTypeRepository contentTypeRepository, ILanguageBranchRepository languageBranchRepository,
        IPageCriteriaQueryable pageCriteriaQueryable, ISiteDefinitionResolver siteDefinitionResolver)
    {
        _routers = routers;
        _logger = logger;
        _rssFeedOptions = rssFeedOptions;
        _contentTypeRepository = contentTypeRepository;
        _languageBranchRepository = languageBranchRepository;
        _pageCriteriaQueryable = pageCriteriaQueryable;
        _siteDefinitionResolver = siteDefinitionResolver;
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

            await foreach (var discoveryResult in ResolveContentTypeFeeds(feedContainerType, feedItemType))
            {
                yield return discoveryResult;
            }
        }
    }

    private async IAsyncEnumerable<FeedDiscoveryResult> ResolveContentTypeFeeds(Type feedContainerType, Type feedItemType)
    {
        var feedContainerOptions = _rssFeedOptions.Get(feedContainerType.Name);
        var feedContainerContentType = _contentTypeRepository.Load(feedContainerType);
        var feedItemContentType = _contentTypeRepository.Load(feedItemType);

        foreach (var languageBranch in _languageBranchRepository.ListEnabled())
        {
            var languageSelector = new LanguageSelector(languageBranch.LanguageID);
            var containerPages = FindContainerPages(feedContainerContentType, languageBranch, languageSelector);

            foreach (var containerPage in containerPages)
            {
                if (!TryGetHostName(containerPage, out var hostNameIdentifier))
                {
                    continue;
                }

                var itemPages = FindItemPages(feedItemContentType, containerPage, languageBranch, languageSelector);

                _sortPublishedDescending.Filter(itemPages);

                // ReSharper disable SuspiciousTypeConversion.Global -> Intentionally enforcing this rule under the initialization logic. We expect these types.
                yield return new FeedDiscoveryResult
                {
                    HostNameIdentifier = hostNameIdentifier,
                    Language = languageBranch.LanguageID,
                    FeedContainer = (IRssFeedSourceContainer)containerPage,
                    FeedItems = itemPages
                        .Take(feedContainerOptions.MaxSyndicationItems ?? DefaultOptions.MaxSyndicationItems ?? 50)
                        .Cast<IRssFeedSourceItem>()
                };
                // ReSharper restore SuspiciousTypeConversion.Global
            }
        }

        // Will force this method to be async enumerable.
        await Task.CompletedTask;
    }

    private bool TryGetHostName(PageData containerPage, out string? hostName)
    {
        var containerSiteDefinition = _siteDefinitionResolver.GetByContent(containerPage.ContentLink, false);
        if (containerSiteDefinition == null)
        {
            _logger.LogWarning("Could not resolve site definition for feed container page '{pageName}'. Skipping feed discovery for this page.", containerPage.Name);

            hostName = null;

            return false;
        }

        hostName = containerSiteDefinition.Id.ToString("N");

        return true;
    }

    private PageDataCollection FindItemPages(ContentType feedItemContentType, PageData containerPage, LanguageBranch languageBranch, LanguageSelector languageSelector)
    {
        var itemCriteria = new PropertyCriteria
        {
            Name = nameof(PropertyPageType.PageTypeID),
            Type = PropertyDataType.PageType,
            Required = true,
            Condition = CompareCondition.Equal,
            Value = feedItemContentType.ID.ToString()
        };
        var itemPages = _pageCriteriaQueryable.FindPagesWithCriteria(containerPage.ContentLink, [itemCriteria], languageBranch.LanguageID, languageSelector);

        return itemPages;
    }

    private PageDataCollection FindContainerPages(ContentType feedContainerContentType, LanguageBranch languageBranch, LanguageSelector languageSelector)
    {
        var containerCriteria = new PropertyCriteria
        {
            Name = nameof(PropertyPageType.PageTypeID),
            Type = PropertyDataType.PageType,
            Required = true,
            Condition = CompareCondition.Equal,
            Value = feedContainerContentType.ID.ToString()
        };

        var containerPages = _pageCriteriaQueryable.FindPagesWithCriteria(ContentReference.RootPage, [containerCriteria], languageBranch.LanguageID, languageSelector);
        return containerPages;
    }

    private Type? GetFeedItemType(Type? feedContainerType, Type routerType)
    {
        var inheritedContainerInterface = feedContainerType?.GetInterfaces().FirstOrDefault(type => type.IsAssignableTo(typeof(IRssFeedSourceContainer)));

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