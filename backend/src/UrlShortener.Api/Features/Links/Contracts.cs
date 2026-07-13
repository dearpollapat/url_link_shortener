using UrlShortener.Api.Core;

namespace UrlShortener.Api.Features.Links;

/// <summary>Request body for creating a link.</summary>
/// <param name="Url">The default destination. Required, must be an absolute http/https URL.</param>
/// <param name="CustomAlias">Optional user-chosen short code; auto-generated when omitted.</param>
/// <param name="Destinations">Optional platform overrides, e.g. { "android": "...", "ios": "..." }.</param>
public sealed record CreateLinkRequest(
    string Url,
    string? CustomAlias = null,
    Dictionary<string, string>? Destinations = null);

/// <summary>Request body for enabling/disabling a link.</summary>
public sealed record UpdateStatusRequest(LinkStatus Status);

/// <summary>Public view of a link, including stats.</summary>
public sealed record LinkResponse(
    string ShortCode,
    string ShortUrl,
    Dictionary<string, string> Destinations,
    string Status,
    long ClickCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastAccessedAt);
