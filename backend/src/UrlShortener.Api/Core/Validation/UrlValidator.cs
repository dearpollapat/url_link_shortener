namespace UrlShortener.Api.Core.Validation;

/// <summary>
/// Accepts absolute http/https URLs only. Rejects other schemes (javascript:,
/// file:, ftp:) so a short link can't be used to smuggle a non-web destination.
/// </summary>
public sealed class UrlValidator : IUrlValidator
{
    public bool TryNormalize(string? url, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
            return false;

        var candidate = url.Trim();

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (string.IsNullOrEmpty(uri.Host))
            return false;

        normalized = uri.ToString();
        return true;
    }
}
