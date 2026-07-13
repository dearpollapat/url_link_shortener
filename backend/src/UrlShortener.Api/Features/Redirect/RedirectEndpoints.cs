using UrlShortener.Api.Core;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.Api.Features.Redirect;

/// <summary>
/// The public redirect endpoint, mapped at the root so short links look like
/// {baseUrl}/{shortCode}.
/// </summary>
public static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{shortCode}", async (
            string shortCode,
            HttpContext http,
            LinkService service,
            IPlatformDetector platformDetector,
            CancellationToken ct) =>
        {
            var platform = platformDetector.Detect(http.Request.Headers.UserAgent);
            var destination = await service.ResolveForRedirectAsync(shortCode, platform, ct);

            // 302 (temporary) — never cached, so every visit hits the server:
            // the click is always counted and platform routing is re-evaluated.
            // 404 for missing/disabled links means they do not redirect.
            return destination is null
                ? Results.NotFound()
                : Results.Redirect(destination, permanent: false);
        }).ExcludeFromDescription();

        return app;
    }
}
