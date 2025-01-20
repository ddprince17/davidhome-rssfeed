namespace DavidHome.RssFeed.Models.Options;

public record RssFeedOptions
{
    public int? ContentMaxLength { get; set; } = 1000000;
    public int? MaxSyndicationItems { get; set; } = 50;
    public bool? SerializeExtensionsAsAtom { get; set; } = false;
}