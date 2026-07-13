using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;

namespace UrlShortener.Api.Features.Links;

/// <summary>
/// Application logic for links: creation (pluggable code generation, validation,
/// collision handling), stats, status changes, deletion, and redirect
/// resolution. Business failures are returned as <see cref="Result"/>/<see
/// cref="Error"/> rather than thrown, so callers handle them explicitly and the
/// happy path stays free of exception control flow.
/// </summary>
public sealed class LinkService
{
    private const int MaxGenerationAttempts = 5;

    private readonly ILinkRepository _repository;
    private readonly ShortCodeGeneratorResolver _resolver;
    private readonly IUrlValidator _urlValidator;
    private readonly TimeProvider _clock;
    private readonly string _shortDomainAuthority;

    public LinkService(
        ILinkRepository repository,
        ShortCodeGeneratorResolver resolver,
        IUrlValidator urlValidator,
        IOptions<ShortUrlOptions> shortUrlOptions,
        TimeProvider clock)
    {
        _repository = repository;
        _resolver = resolver;
        _urlValidator = urlValidator;
        _clock = clock;

        // Authority (host:port) of our own short domain, used to reject links
        // that would point back at us and cause a redirect loop.
        _shortDomainAuthority = Uri.TryCreate(shortUrlOptions.Value.BaseUrl, UriKind.Absolute, out var baseUri)
            ? baseUri.Authority
            : string.Empty;
    }

    public async Task<Result<Link>> CreateAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        var destinationsResult = BuildDestinations(request);
        if (destinationsResult.IsFailure)
            return destinationsResult.Error;

        var destinations = destinationsResult.Value;
        var genRequest = new GenerationRequest(request.CustomAlias);
        var generator = _resolver.Resolve(genRequest);
        var isDeterministic = !generator.AllowRetryOnCollision; // custom alias

        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var code = generator.Generate(genRequest);

            if (!AliasRules.IsWellFormed(code))
            {
                if (isDeterministic)
                    return Error.Validation("Alias must be 3–30 characters: letters, digits, '-' or '_'.");
                continue; // random code was somehow malformed — try again (defensive)
            }

            if (AliasRules.IsReserved(code))
            {
                if (isDeterministic)
                    return Error.Validation($"'{code}' is a reserved name and cannot be used as an alias.");
                continue;
            }

            var link = new Link(code, destinations, _clock.GetUtcNow());

            if (await _repository.TryAddAsync(link, ct))
                return link;

            // Deterministic strategies can't escape a collision by retrying.
            if (isDeterministic)
                return Error.Conflict($"Alias '{code}' is already in use.");
        }

        return Error.Validation("Could not generate a unique short code. Please try again.");
    }

    public Task<Link?> GetAsync(string shortCode, CancellationToken ct = default) =>
        _repository.GetAsync(shortCode, ct);

    public Task<IReadOnlyCollection<Link>> GetAllAsync(CancellationToken ct = default) =>
        _repository.GetAllAsync(ct);

    public async Task<Result<Link>> SetStatusAsync(string shortCode, LinkStatus status, CancellationToken ct = default)
    {
        var link = await _repository.GetAsync(shortCode, ct);
        if (link is null)
            return Error.NotFound($"Link '{shortCode}' was not found.");

        link.SetStatus(status);
        await _repository.UpdateAsync(link, ct);
        return link;
    }

    public async Task<Result> DeleteAsync(string shortCode, CancellationToken ct = default)
    {
        var removed = await _repository.RemoveAsync(shortCode, ct);
        return removed ? Result.Success() : Error.NotFound($"Link '{shortCode}' was not found.");
    }

    /// <summary>
    /// Resolves the destination for a visit and records the click. Returns null
    /// when the link is missing or disabled, so the caller responds 404 rather
    /// than redirecting. (Binary outcome with no message — a plain nullable is
    /// clearer here than a Result.)
    /// </summary>
    public async Task<string?> ResolveForRedirectAsync(string shortCode, Platform platform, CancellationToken ct = default)
    {
        var link = await _repository.GetAsync(shortCode, ct);
        if (link is null || link.Status != LinkStatus.Active)
            return null;

        link.RegisterVisit(_clock.GetUtcNow());
        await _repository.UpdateAsync(link, ct);
        return link.ResolveDestination(platform);
    }

    private Result<Dictionary<Platform, string>> BuildDestinations(CreateLinkRequest request)
    {
        var defaultResult = ValidateDestination(request.Url);
        if (defaultResult.IsFailure)
            return defaultResult.Error;

        var destinations = new Dictionary<Platform, string> { [Platform.Default] = defaultResult.Value };

        if (request.Destinations is null)
            return destinations;

        foreach (var (key, value) in request.Destinations)
        {
            if (!Enum.TryParse<Platform>(key, ignoreCase: true, out var platform))
                return Error.Validation($"Unknown platform '{key}'. Valid values: default, ios, android.");

            if (platform == Platform.Default)
                continue; // the default destination always comes from Url

            var result = ValidateDestination(value);
            if (result.IsFailure)
                return Error.Validation($"Destination for '{key}': {result.Error.Message}");

            destinations[platform] = result.Value;
        }

        return destinations;
    }

    private Result<string> ValidateDestination(string? url)
    {
        if (!_urlValidator.TryNormalize(url, out var normalized))
            return Error.Validation("A valid absolute http/https URL is required.");

        // Reject links that resolve back to our own domain to avoid redirect loops.
        var uri = new Uri(normalized);
        if (!string.IsNullOrEmpty(_shortDomainAuthority) &&
            string.Equals(uri.Authority, _shortDomainAuthority, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Destination cannot point back to the shortener's own domain.");
        }

        return normalized;
    }
}
