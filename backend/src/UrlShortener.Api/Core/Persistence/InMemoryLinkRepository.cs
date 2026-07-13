using System.Collections.Concurrent;

namespace UrlShortener.Api.Core.Persistence;

/// <summary>
/// Thread-safe in-memory store. Short codes are matched case-insensitively so
/// "MyLink" and "mylink" cannot both be reserved. Registered as a singleton so
/// state survives across requests for the process lifetime.
/// </summary>
public sealed class InMemoryLinkRepository : ILinkRepository
{
    private readonly ConcurrentDictionary<string, Link> _links =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> TryAddAsync(Link link, CancellationToken ct = default) =>
        Task.FromResult(_links.TryAdd(link.ShortCode, link));

    public Task<Link?> GetAsync(string shortCode, CancellationToken ct = default) =>
        Task.FromResult(_links.GetValueOrDefault(shortCode));

    public Task<IReadOnlyCollection<Link>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<Link>>(_links.Values.ToList());

    public Task UpdateAsync(Link link, CancellationToken ct = default)
    {
        // The dictionary already holds this reference, so in-place mutations
        // (Interlocked click count, status) are already visible. We must NOT use
        // the indexer here: `_links[code] = link` is an add-or-update and would
        // resurrect a link removed by a concurrent DeleteAsync between read and
        // update. TryUpdate only writes when the key is still present.
        _links.TryUpdate(link.ShortCode, link, link);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string shortCode, CancellationToken ct = default) =>
        Task.FromResult(_links.TryRemove(shortCode, out _));
}
