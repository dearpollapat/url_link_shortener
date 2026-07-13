namespace UrlShortener.Api.Core;

/// <summary>
/// Lightweight User-Agent sniffing. Deliberately simple substring matching —
/// enough for iOS vs Android vs everything-else. Unknown or missing agents
/// (bots, curl) fall back to <see cref="Platform.Default"/> so a link always
/// resolves.
/// </summary>
public sealed class UserAgentPlatformDetector : IPlatformDetector
{
    public Platform Detect(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return Platform.Default;

        // iPadOS reports "Macintosh" in some modes; "iphone"/"ipad"/"ipod" are the reliable markers.
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return Platform.Android;

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase))
            return Platform.iOS;

        return Platform.Default;
    }
}
