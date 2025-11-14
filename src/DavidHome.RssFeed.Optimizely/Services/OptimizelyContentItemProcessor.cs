using System.ServiceModel.Syndication;
using DavidHome.RssFeed.Contracts;
using DavidHome.RssFeed.Contracts.Extensions;
using DavidHome.RssFeed.Models;
using DavidHome.RssFeed.Optimizely.Contracts;
using DavidHome.RssFeed.Optimizely.Models;
using DavidHome.RssFeed.Optimizely.Models.Extensions;
using DavidHome.RssFeed.Optimizely.Models.Options;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;

namespace DavidHome.RssFeed.Optimizely.Services;

public class OptimizelyContentItemProcessor : OptimizelyProcessorBase, IRssFeedItemProcessor
{
    private readonly CategoryRepository _categoryRepository;
    private readonly IContentVersionRepository _contentVersionRepository;
    private readonly IOptimizelyContentAreaService _optimizelyContentAreaService;
    private readonly IOptionsMonitor<RssFeedOptimizelyOptions> _feedOptions;
    private RssFeedOptimizelyOptions DefaultFeedOptions => _feedOptions.CurrentValue;
    private RssFeedOptimizelyOptions ContainerFeedOptions(string? containerName) => _feedOptions.Get(containerName);

    public OptimizelyContentItemProcessor(IUrlResolver urlResolver, CategoryRepository categoryRepository, IContentVersionRepository contentVersionRepository,
        IOptimizelyContentAreaService optimizelyContentAreaService, IOptionsMonitor<RssFeedOptimizelyOptions> feedOptions) : base(urlResolver)
    {
        _categoryRepository = categoryRepository;
        _contentVersionRepository = contentVersionRepository;
        _optimizelyContentAreaService = optimizelyContentAreaService;
        _feedOptions = feedOptions;
    }

    public Task<bool> IsValidFeedModel(IRssFeedSourceBase? feedModel)
    {
        return Task.FromResult(true);
    }

    public Task PreProcess(IRssFeedSourceBase? feedModel)
    {
        TransformSource(ref feedModel);

        var rssFeedItem = feedModel as IRssFeedItem;
        var content = (rssFeedItem as IContentRssFeed)?.Content;

        ProcessCommonOptimizelyProperties(rssFeedItem);
        AddItemCategories(rssFeedItem, content);
        SetFeedItemContent(rssFeedItem, content);
        // TODO: Will be completed in a later release.
        // rssFeedItem.Authors = CreateContentAuthors(content);

        return Task.CompletedTask;
    }

    public Task PostProcess(IRssFeedBase? feedModel, object? syndicationModel)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global -> This is expected and enforced in the initialization.
        if (feedModel is not IContentRssFeed { Content: IVersionable versionable } || syndicationModel is not SyndicationItem syndicationItem)
        {
            return Task.CompletedTask;
        }

        if (versionable.StartPublish != null)
        {
            syndicationItem.PublishDate = versionable.StartPublish.Value.ToUniversalTime();
        }

        return Task.CompletedTask;
    }

    private void AddItemCategories(IRssFeedItem? rssFeedItem, IContent? content)
    {
        var categories = content is ICategorizable categorizable
            ? categorizable.Category
                .Select(x => _categoryRepository.Get(x))
                .Where(category => category != null)
                .Select(category => new SyndicationCategory(category.Name))
            : [];

        // We're just making sure we aren't adding the same categories twice.
        // Optimizely seems to be keeping ignored properties with their values intact throughout the whole application lifecycle.
        rssFeedItem?.RssCategories ??= new List<SyndicationCategory?>();
        rssFeedItem?.RssCategories?.Clear();

        foreach (var category in categories)
        {
            rssFeedItem?.RssCategories?.Add(category);
        }
    }

    private void SetFeedItemContent(IRssFeedItem? rssFeedItem, IContent? content)
    {
        var feedItemType = rssFeedItem?.GetType().GetInterfaces().FirstOrDefault(type => type.IsAssignableTo(typeof(IRssFeedItem)));
        Type? containerType = null;

        if (feedItemType is { IsConstructedGenericType: true })
        {
            containerType = feedItemType.GenericTypeArguments.First();
        }

        var contentPropertyName = ContainerFeedOptions(containerType?.Name).ContentAreaPropertyName ?? DefaultFeedOptions.ContentAreaPropertyName;

        if (string.IsNullOrEmpty(contentPropertyName))
        {
            return;
        }

        var contentArea = content?.Property[contentPropertyName]?.Value as ContentArea;
        var contentHtml = _optimizelyContentAreaService.RenderAsString(contentArea);
        var contentMaxLength = ContainerFeedOptions(containerType?.Name).ContentMaxLength ?? DefaultFeedOptions.ContentMaxLength ?? 1000000;

        rssFeedItem?.RssContent = new TextSyndicationContent(contentHtml.Ellipsis(contentMaxLength), TextSyndicationContentKind.Html);
    }

    private ICollection<SyndicationPerson?> CreateContentAuthors(IContent content)
    {
        var versionFilter = new VersionFilter
        {
            ContentLink = content.ContentLink, Statuses = [VersionStatus.Published, VersionStatus.PreviouslyPublished], ExcludeDeleted = true
        };

        if (content is ILocalizable localizable)
        {
            versionFilter.Languages = [localizable.Language];
        }

        var contentVersions = _contentVersionRepository.List(versionFilter, 0, int.MaxValue, out _);
        // A syndication person requires an email address. This is not what the SavedBy contains. 
        // var contentAuthors = contentVersions.Select(version => version.SavedBy).Distinct().Select(name => new SyndicationPerson(name));

        throw new NotImplementedException();
    }

    public void TransformSource(ref IRssFeedSourceBase? feedModel)
    {
        feedModel = feedModel?.TransformToFeedItem();
    }
}