using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace DavidHome.RssFeed.Optimizely.Helpers;

public class HtmlStringWriter : StringWriter
{
    public HtmlStringWriter()
    {
    }

    public HtmlStringWriter(IFormatProvider? formatProvider) : base(formatProvider)
    {
    }

    public HtmlStringWriter(StringBuilder sb) : base(sb)
    {
    }

    public HtmlStringWriter(StringBuilder sb, IFormatProvider? formatProvider) : base(sb, formatProvider)
    {
    }

    public override void Write(object? value)
    {
        if (value is IHtmlContent htmlContent)
        {
            htmlContent.WriteTo(this, HtmlEncoder.Default);
        }
        else
        {
            base.Write(value);
        }
    }
}