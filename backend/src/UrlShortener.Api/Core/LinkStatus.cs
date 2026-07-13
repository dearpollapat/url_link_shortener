namespace UrlShortener.Api.Core;

/// <summary>
/// Lifecycle state of a link. Modeled as an enum rather than a boolean flag so
/// new states (e.g. Expired) can be added without changing the redirect check.
/// </summary>
public enum LinkStatus
{
    Active,
    Disabled
}
