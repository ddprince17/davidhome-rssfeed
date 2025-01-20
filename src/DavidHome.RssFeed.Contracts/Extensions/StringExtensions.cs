namespace DavidHome.RssFeed.Contracts.Extensions;

public static class StringExtensions
{
    public static string Ellipsis(this string? text, int length)
    {
        text ??= string.Empty;

        return text.Length <= length ? text : $"{text[..length]}...";
    }
}