using Microsoft.AspNetCore.Html;

namespace DavidHome.RssFeed.Optimizely.Contracts;

public interface IOptimizelySyndicationLinkService
{
    IHtmlContent GenerateSyndicationLink();
}