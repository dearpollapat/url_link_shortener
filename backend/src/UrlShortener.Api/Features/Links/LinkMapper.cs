using UrlShortener.Api.Core;

namespace UrlShortener.Api.Features.Links;

/// <summary>Maps the <see cref="Link"/> domain model to its API representation.</summary>
public static class LinkMapper
{
    public static LinkResponse ToResponse(this Link link, string baseUrl) => new(
        ShortCode: link.ShortCode,
        ShortUrl: $"{baseUrl.TrimEnd('/')}/{link.ShortCode}",
        Destinations: link.Destinations.ToDictionary(
            kvp => kvp.Key.ToString().ToLowerInvariant(),
            kvp => kvp.Value),
        Status: link.Status.ToString(),
        ClickCount: link.ClickCount,
        CreatedAt: link.CreatedAt,
        LastAccessedAt: link.LastAccessedAt);
}
