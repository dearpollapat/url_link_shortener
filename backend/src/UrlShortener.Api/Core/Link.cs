namespace UrlShortener.Api.Core;

/// <summary>
/// A shortened link and its platform-specific destinations.
/// Click counting mutates <see cref="ClickCount"/> and <see cref="LastAccessedAt"/>;
/// those writes go through <see cref="RegisterVisit"/> so the concurrency
/// guarantee lives in one place.
/// </summary>
public sealed class Link
{
    private long _clickCount;

    public Link(string shortCode, IReadOnlyDictionary<Platform, string> destinations, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            throw new ArgumentException("Short code is required.", nameof(shortCode));
        if (destinations is null || destinations.Count == 0)
            throw new ArgumentException("At least one destination is required.", nameof(destinations));
        if (!destinations.ContainsKey(Platform.Default))
            throw new ArgumentException("A Default destination is required.", nameof(destinations));

        ShortCode = shortCode;
        Destinations = destinations;
        CreatedAt = createdAt;
        Status = LinkStatus.Active;
    }

    public string ShortCode { get; }

    public IReadOnlyDictionary<Platform, string> Destinations { get; }

    public LinkStatus Status { get; private set; }

    public long ClickCount => Interlocked.Read(ref _clickCount);

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? LastAccessedAt { get; private set; }

    /// <summary>Resolves the destination for a platform, falling back to Default.</summary>
    public string ResolveDestination(Platform platform) =>
        Destinations.TryGetValue(platform, out var url) ? url : Destinations[Platform.Default];

    /// <summary>
    /// Atomically increments the click count and records the access time.
    /// Safe to call concurrently from multiple redirect requests.
    /// </summary>
    public void RegisterVisit(DateTimeOffset at)
    {
        Interlocked.Increment(ref _clickCount);
        LastAccessedAt = at;
    }

    public void SetStatus(LinkStatus status) => Status = status;
}
