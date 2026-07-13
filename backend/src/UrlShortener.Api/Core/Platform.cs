namespace UrlShortener.Api.Core;

/// <summary>
/// Visitor platform, decided at redirect time from the User-Agent header.
/// <c>Default</c> is the fallback destination that every link must have.
/// </summary>
public enum Platform
{
    Default,
    iOS,
    Android
}
