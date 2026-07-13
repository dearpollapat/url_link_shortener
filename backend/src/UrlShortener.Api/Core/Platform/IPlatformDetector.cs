namespace UrlShortener.Api.Core;

/// <summary>
/// Decides the visitor platform from the User-Agent header. Kept separate from
/// the link/redirect logic so User-Agent parsing can be tested and swapped
/// (e.g. for a richer device-detection library) on its own.
/// </summary>
public interface IPlatformDetector
{
    Platform Detect(string? userAgent);
}
