namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Strategy for user-supplied aliases. It only produces the candidate (the
/// trimmed alias); shape and reserved-name rules are enforced centrally by the
/// service via <see cref="AliasRules"/>. Because the code is deterministic, a
/// collision is a real conflict — <see cref="AllowRetryOnCollision"/> is false.
/// </summary>
public sealed class CustomAliasGenerator : IShortCodeGenerator
{
    public string Name => "custom-alias";

    public bool AllowRetryOnCollision => false;

    public bool CanHandle(GenerationRequest request) =>
        !string.IsNullOrWhiteSpace(request.CustomAlias);

    public string Generate(GenerationRequest request) => request.CustomAlias!.Trim();
}
