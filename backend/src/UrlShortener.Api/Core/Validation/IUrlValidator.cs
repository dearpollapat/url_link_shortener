namespace UrlShortener.Api.Core.Validation;

/// <summary>Validates and normalizes destination URLs before a link is created.</summary>
public interface IUrlValidator
{
    /// <summary>
    /// Returns true and the normalized absolute URL when valid; false otherwise.
    /// </summary>
    bool TryNormalize(string? url, out string normalized);
}
