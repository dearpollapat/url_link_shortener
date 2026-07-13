namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Picks the first registered generator that can handle a request. Registration
/// order matters: specific strategies (e.g. custom alias) come first, the
/// catch-all random generator last.
/// </summary>
public sealed class ShortCodeGeneratorResolver
{
    private readonly IReadOnlyList<IShortCodeGenerator> _generators;

    public ShortCodeGeneratorResolver(IEnumerable<IShortCodeGenerator> generators)
    {
        _generators = generators.ToList();
        if (_generators.Count == 0)
            throw new InvalidOperationException("No short-code generators are registered.");
    }

    public IShortCodeGenerator Resolve(GenerationRequest request) =>
        _generators.FirstOrDefault(g => g.CanHandle(request))
        ?? throw new InvalidOperationException("No generator can handle the request.");
}
