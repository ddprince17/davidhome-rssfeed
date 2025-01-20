using System.Text;
using DavidHome.RssFeed.Optimizely.Contracts;
using DavidHome.RssFeed.Optimizely.Helpers;
using EPiServer.Core;
using EPiServer.Web.Mvc.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.RssFeed.Optimizely.Services;

public class OptimizelyContentAreaService : IOptimizelyContentAreaService
{
    private readonly IHtmlHelper _htmlHelper;
    private readonly ContentAreaRenderer _contentAreaRenderer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITempDataProvider _tempDataProvider;

    public OptimizelyContentAreaService(IHtmlHelper htmlHelper, ContentAreaRenderer contentAreaRenderer, IServiceProvider serviceProvider, ITempDataProvider tempDataProvider)
    {
        _htmlHelper = htmlHelper;
        _contentAreaRenderer = contentAreaRenderer;
        _serviceProvider = serviceProvider;
        _tempDataProvider = tempDataProvider;
    }

    public virtual string? RenderAsString(ContentArea? contentArea, IEnumerable<KeyValuePair<string, object?>>? routeValues = null)
    {
        RouteValueDictionary routeData = new();
        
        if (routeValues != null)
        {
            foreach (var (key, value) in routeValues)
            {
                routeData.Add(key, value);
            }
        }
        
        using var serviceScope = _serviceProvider.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = serviceScope.ServiceProvider, Request = { RouteValues = routeData } };
        var stringBuilder = new StringBuilder();
        using var writer = new HtmlStringWriter(stringBuilder);
        var viewContext = new ViewContext
        {
            Writer = writer, HttpContext = httpContext, RouteData = new RouteData(), ActionDescriptor = new ActionDescriptor(),
            TempData = new TempDataDictionary(httpContext, _tempDataProvider)
        };

        (_htmlHelper as IViewContextAware)?.Contextualize(viewContext);
        _contentAreaRenderer.Render(_htmlHelper, contentArea);

        return stringBuilder.ToString();
    }
}