namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>Input a generator needs to produce a short code.</summary>
public sealed record GenerationRequest(string? CustomAlias);
