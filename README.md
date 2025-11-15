⚠️ Important: This release introduces breaking API changes since 1.2.0 (interfaces and method signatures for builders, processors, and storage providers). See `CHANGELOG` for migration details.

## Packages you need
- `DavidHome.RssFeed` is the base library that contains builders, processors, discovery contracts and models.
- `DavidHome.RssFeed.Optimizely` adds Optimizely discovery, processors, routing and the scheduled job described below.
- `DavidHome.RssFeed.Storage.AzureBlob` persists generated feeds in Azure Blob Storage.

Install the packages your scenario requires (for Optimizely you typically reference all three).

## Registering the services
Add the feed services during startup:

```csharp
using DavidHome.RssFeed.Contracts;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddDavidHomeRssFeed(Configuration)
            .AddOptimizelyFeedIntegration(Configuration)
            .AddDefaultOptimizelyProcessors()
            // Register each feed container/feed item pair that should expose a feed.
            .AddContentPageFeed<ArticleFeedContainerPage, ArticleFeedItemPage>(
                Configuration,
                new[] { typeof(ArticleFeedContainerPage).Assembly, typeof(ArticleFeedItemProcessor).Assembly })
            // Optional storage provider(s).
            .AddAzureBlobStorage(Configuration.GetSection("ConnectionStrings:EPiServerAzureBlobs"));
    }

    public void Configure(IApplicationBuilder app)
    {
        // Ensures the Blob container exists at startup.
        app.UseAzureBlobRssFeed();
    }
}
```

Key points:
- `AddDavidHomeRssFeed` wires `IRssFeedBuilder` and binds `DavidHome:RssFeed` options.
- `AddOptimizelyFeedIntegration` registers discovery, routing and Optimizely helper services.
- `AddDefaultOptimizelyProcessors` adds the provided processors. Call it **before** `AddContentPageFeed` if you want to override any processor registrations with your own implementations.
- `AddContentPageFeed<TContainer, TItem>(..., params IReadOnlyCollection<Assembly> assembliesToScan)` associates a container/item pair and scans assemblies for `IRssFeedContainerProcessor`/`IRssFeedItemProcessor` implementations. Pass every assembly that contains processors you want registered.
- Register one or many `IRssFeedStorageProvider` implementations (`AddAzureBlobStorage` shown above, or a custom provider).

## Content type setup
For each pair of content types that should produce a feed:

1. Implement the marker interfaces `IRssFeedSourceContainer<TFeedItem>` on the container type and `IRssFeedSourceItem<TFeedContainer>` on the item type. These are the types that `AddContentPageFeed` registers.
2. Implement `IContentRssFeed` so the Optimizely processors can access `IContent`. No RSS-specific properties need to be added to the content types in 2.0.0.

```csharp
[ContentType(DisplayName = "Article Feed Container", GUID = "00000000-0000-0000-0000-000000000001")]
public class ArticleFeedContainerPage : PageData, IRssFeedSourceContainer<ArticlePage>, IContentRssFeed
{
}

[ContentType(DisplayName = "Article", GUID = "00000000-0000-0000-0000-000000000002")]
public class ArticlePage : PageData, IRssFeedSourceItem<ArticleFeedContainerPage>, IContentRssFeed
{
}
```

The default processors (`OptimizelyContentContainerProcessor` and `OptimizelyContentItemProcessor`) convert your Optimizely content into the runtime `IRssFeedContainer`/`IRssFeedItem` implementations and set all required metadata at runtime, but you can supply additional processors to enrich or override values. If you define a processor with generic parameters that match the feed types (`IRssFeedItemProcessor<ArticlePage>` for example), register it in one of the assemblies supplied to `AddContentPageFeed`.

## Configuration reference
The library reads its settings from `DavidHome:RssFeed` in your configuration. A single set of defaults applies to every feed, and a child object named after a container type overrides values for that feed only.

```json
{
  "DavidHome": {
    "RssFeed": {
      "SerializeExtensionsAsAtom": false,
      "ContentMaxLength": 25000000,
      "MaxSyndicationItems": 50,
      "ContentAreaPropertyName": "MainContentArea",
      "ArticleFeedContainerPage": {
        "ContentMaxLength": 15000000,
        "ContentAreaPropertyName": "MainContentArea",
        "FeedTitlePropertyName": "HeadTitle",
        "MaxSyndicationItems": 25
      }
    }
  }
}
```

| Name | Description | Default |
|--|--|--|
| ContentMaxLength | Max number of characters that will be written to the `<description>` for each item. | 1000000 |
| MaxSyndicationItems | Maximum number of items emitted per feed. | 50 |
| SerializeExtensionsAsAtom | When `true`, extensions are serialized using the Atom namespace. | false |

Optimizely-specific overrides (placed either at root or under the container type name):

| Name | Description | Default |
|--|--|--|
| FeedRelativeUrl | Relative segment appended to the container page URL (supports multiple levels such as `"rss/latest"`). | `"rss"` |
| ContentAreaPropertyName | Content area used to generate item body HTML. Leave null to skip body generation. | null |
| FeedTitlePropertyName | Fallback property name for the feed title. Defaults to `Name`. | null |

### Azure Blob configuration
When using `DavidHome.RssFeed.Storage.AzureBlob`, provide the connection string section to `AddAzureBlobStorage`. For Optimizely DXP you can re-use the built-in connection string:

```csharp
var blobConnection = Configuration.GetSection("ConnectionStrings").GetSection("EPiServerAzureBlobs");
services.AddDavidHomeRssFeed(Configuration)
        // ...
        .AddAzureBlobStorage(blobConnection);
```

Also ensure the project references a recent `Microsoft.Extensions.Azure` package (the helper does not work with older versions).

## Building and publishing feeds
Call `IRssFeedBuilder.BuildFeeds()` to generate feeds for every registered container. It returns an `IAsyncEnumerable<SyndicationFeedResult?>`, so you must `await foreach` the sequence and expect null entries whenever processors detect invalid data.

The Optimizely package exposes a ready-to-use scheduled job (`RssFeedGeneratorScheduledJob`). Once the assembly is referenced, enable the job from the Optimizely admin UI to run feed generation on an interval. The job simply iterates the feeds and calls every registered `IRssFeedStorageProvider`:

`IRssFeedStorageProvider` implementations are multi-language and host aware starting in 2.0.0, so be sure to pass the `Language` and `HostNameIdentifier` values everywhere you call `Save` or `GetSavedStream`. The Azure Blob provider stores blobs under `<host>/<language>/<feedId>.xml`.

## Serving feeds to clients
- Partial routing for each container is automatically registered when calling `AddContentPageFeed`. Visiting `/path/to/container/<FeedRelativeUrl>` returns the feed.
- The Optimizely package already includes `RssFeedDataController`, so once the NuGet is referenced and the site is rebuilt you automatically get the controller that streams the generated XML from every registered `IRssFeedStorageProvider`.
- To add the RSS `<link>` element to the HTML `<head>` of a container page, call the provided HTML helper: `@Html.SyndicationLink()` (from `DavidHome.RssFeed.Optimizely.Contracts.Extensions`). It renders markup such as `<link href="https://www.example.com/rss/" rel="alternate" title="My Articles" type="application/rss+xml">`.
- Because discovery now runs per enabled language and per site host, ensure each feed container has published content for the languages/hosts you expect. That guarantees the builder returns one `SyndicationFeedResult` per `(container, host, language)` combination and that the partial router can resolve `FeedRelativeUrl` correctly.
