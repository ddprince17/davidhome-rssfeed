using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.RssFeed.Optimizely.Contracts.Extensions;

public static class HtmlHelperExtensions
{
    public static IHtmlContent SyndicationLink(this IHtmlHelper helper)
    {
        var syndicationLinkService = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IOptimizelySyndicationLinkService>();

        return syndicationLinkService.GenerateSyndicationLink();
    }
}