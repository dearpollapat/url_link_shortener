namespace UrlShortener.Api.Core.Persistence;

/// <summary>
/// Storage abstraction for links. The current implementation is in-memory;
/// this seam lets it be swapped for a database (EF Core, etc.) without touching
/// the service layer. Async by design so a real, I/O-bound store fits the same
/// contract.
/// </summary>
public interface ILinkRepository
{
    /// <summary>
    /// Atomically reserves <paramref name="link"/> under its short code.
    /// Returns false if the code is already taken — this is the single point
    /// where uniqueness is enforced, so it must be atomic under concurrency.
    /// </summary>
    Task<bool> TryAddAsync(Link link, CancellationToken ct = default);

    Task<Link?> GetAsync(string shortCode, CancellationToken ct = default);

    Task<IReadOnlyCollection<Link>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Persists changes to an existing link (status, click count). A no-op for
    /// the in-memory store since it holds the same reference, but the seam a
    /// database implementation needs.
    /// </summary>
    Task UpdateAsync(Link link, CancellationToken ct = default);

    Task<bool> RemoveAsync(string shortCode, CancellationToken ct = default);
}
