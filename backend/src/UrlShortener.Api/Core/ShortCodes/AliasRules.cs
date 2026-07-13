using System.Text.RegularExpressions;

namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Rules a short code must satisfy regardless of how it was produced. Applied by
/// the service to every candidate, so both custom aliases and (defensively)
/// auto-generated codes are checked in one place.
/// </summary>
public static partial class AliasRules
{
    // Names that would shadow real routes served at the root, so a short code
    // must never take them. Matched case-insensitively (routing is too).
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "openapi", "swagger", "health", "healthz",
        "assets", "static", "public", "favicon.ico", "robots.txt", "sitemap.xml"
    };

    /// <summary>3–30 chars: letters, digits, hyphen, underscore.</summary>
    [GeneratedRegex("^[A-Za-z0-9_-]{3,30}$")]
    public static partial Regex WellFormed();

    public static bool IsWellFormed(string code) => WellFormed().IsMatch(code);

    public static bool IsReserved(string code) => Reserved.Contains(code);
}
