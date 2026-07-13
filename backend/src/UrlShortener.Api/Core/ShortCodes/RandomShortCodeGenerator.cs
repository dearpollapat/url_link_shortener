using System.Security.Cryptography;

namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Default strategy: a cryptographically-random code. Unpredictable (unlike a
/// Base62-of-sequential-id scheme) so links can't be enumerated, and it needs
/// no central counter to coordinate. Handles any request, so it is the
/// fallback and must be registered last.
/// </summary>
public sealed class RandomShortCodeGenerator : IShortCodeGenerator
{
    // Omits look-alike characters (0/O, 1/l/I) to stay readable when typed by hand.
    private const string Alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 7;

    public string Name => "random";

    public bool AllowRetryOnCollision => true;

    public bool CanHandle(GenerationRequest request) => true;

    public string Generate(GenerationRequest request)
    {
        Span<char> buffer = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(buffer);
    }
}
