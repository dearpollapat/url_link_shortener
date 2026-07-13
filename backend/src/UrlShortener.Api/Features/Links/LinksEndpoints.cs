using Microsoft.Extensions.Options;
using UrlShortener.Api.Common;

namespace UrlShortener.Api.Features.Links;

/// <summary>Management endpoints for links, grouped under /api/links.</summary>
public static class LinksEndpoints
{
    public static IEndpointRouteBuilder MapLinksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/links").WithTags("Links");

        // Create
        group.MapPost("/", async (
            CreateLinkRequest request,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            if (result.IsFailure)
                return result.Error.ToProblem();

            var response = result.Value.ToResponse(options.Value.BaseUrl);
            return Results.Created($"/api/links/{result.Value.ShortCode}", response);
        });

        // List
        group.MapGet("/", async (
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var links = await service.GetAllAsync(ct);
            var baseUrl = options.Value.BaseUrl;
            return Results.Ok(links
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.ToResponse(baseUrl)));
        });

        // Stats for one link
        group.MapGet("/{shortCode}", async (
            string shortCode,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var link = await service.GetAsync(shortCode, ct);
            return link is null
                ? Results.NotFound()
                : Results.Ok(link.ToResponse(options.Value.BaseUrl));
        });

        // Enable / disable
        group.MapPatch("/{shortCode}", async (
            string shortCode,
            UpdateStatusRequest request,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var result = await service.SetStatusAsync(shortCode, request.Status, ct);
            return result.IsFailure
                ? result.Error.ToProblem()
                : Results.Ok(result.Value.ToResponse(options.Value.BaseUrl));
        });

        // Delete
        group.MapDelete("/{shortCode}", async (
            string shortCode,
            LinkService service,
            CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(shortCode, ct);
            return result.IsFailure ? result.Error.ToProblem() : Results.NoContent();
        });

        return app;
    }
}
