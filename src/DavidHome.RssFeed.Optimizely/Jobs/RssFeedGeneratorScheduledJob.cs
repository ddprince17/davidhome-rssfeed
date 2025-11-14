using DavidHome.RssFeed.Contracts;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace DavidHome.RssFeed.Optimizely.Jobs;

[ScheduledPlugIn(DisplayName = "DavidHome - RSS Feed Generator", Description = "Generates the RSS feed for the website.", Restartable = true,
    IntervalType = ScheduledIntervalType.Hours, IntervalLength = 1, GUID = "7F803FF7-FE55-49B9-B8D0-BEE5B8FCEDA9")]
public class RssFeedGeneratorScheduledJob : ScheduledJobBase
{
    private readonly IRssFeedBuilder _rssFeedBuilder;
    private readonly IEnumerable<IRssFeedStorageProvider> _rssFeedStorageProviders;

    public RssFeedGeneratorScheduledJob(IRssFeedBuilder rssFeedBuilder, IEnumerable<IRssFeedStorageProvider> rssFeedStorageProviders)
    {
        _rssFeedBuilder = rssFeedBuilder;
        _rssFeedStorageProviders = rssFeedStorageProviders;

        IsStoppable = true;
    }

    public override string Execute()
    {
        Task.Run(ExecuteInternal).GetAwaiter().GetResult();

        return "RSS feed generation completed.";
    }

    private async Task ExecuteInternal()
    {
        var feedResults = _rssFeedBuilder.BuildFeeds();

        await foreach (var feedResult in feedResults)
        {
            foreach (var rssFeedStorageProvider in _rssFeedStorageProviders)
            {
                if (feedResult is null)
                {
                    continue;
                }

                await rssFeedStorageProvider.Save(feedResult.Feed, feedResult.Id, feedResult.Language, feedResult.HostNameIdentifier);
            }
        }
    }
}