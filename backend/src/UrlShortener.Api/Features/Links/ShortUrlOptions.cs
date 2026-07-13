namespace UrlShortener.Api.Features.Links;

/// <summary>
/// Configurable base URL for generated short links (e.g. "https://gul.fy").
/// Bound from the "ShortUrl" section so the display domain isn't hard-coded.
/// </summary>
public sealed class ShortUrlOptions
{
    public const string SectionName = "ShortUrl";

    public string BaseUrl { get; set; } = "http://localhost:5000";
}
