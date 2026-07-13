namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// A strategy for producing short codes. New strategies are added by
/// implementing this interface and registering it in DI — no existing code
/// changes (Open/Closed). The resolver selects one at runtime via
/// <see cref="CanHandle"/>.
/// </summary>
public interface IShortCodeGenerator
{
    /// <summary>Identifier used for diagnostics and to tell strategies apart.</summary>
    string Name { get; }

    /// <summary>Whether this strategy is responsible for the given request.</summary>
    bool CanHandle(GenerationRequest request);

    /// <summary>
    /// Whether a collision should be retried with a fresh code. True for random
    /// (a new attempt yields a different code); false for deterministic
    /// strategies like custom aliases, where a collision is a real conflict.
    /// </summary>
    bool AllowRetryOnCollision { get; }

    /// <summary>
    /// Produces a candidate code. Uniqueness is NOT guaranteed here — the
    /// caller reserves the code and retries on collision.
    /// </summary>
    string Generate(GenerationRequest request);
}
