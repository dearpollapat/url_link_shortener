using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class RandomShortCodeGeneratorTests
{
    private readonly RandomShortCodeGenerator _generator = new();
    private static readonly GenerationRequest AnyRequest = new(CustomAlias: null);

    [Fact]
    public void Generate_produces_a_seven_character_code()
    {
        _generator.Generate(AnyRequest).Should().HaveLength(7);
    }

    [Fact]
    public void Generate_uses_only_the_unambiguous_alphabet()
    {
        const string forbidden = "0O1lI";

        var codes = Enumerable.Range(0, 100).Select(_ => _generator.Generate(AnyRequest));

        codes.Should().OnlyContain(code => !code.Any(forbidden.Contains));
    }

    [Fact]
    public void Generate_is_effectively_unique_across_many_calls()
    {
        var codes = Enumerable.Range(0, 10_000)
            .Select(_ => _generator.Generate(AnyRequest))
            .ToList();

        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CanHandle_is_always_true_so_it_acts_as_the_fallback()
    {
        _generator.CanHandle(AnyRequest).Should().BeTrue();
        _generator.CanHandle(new GenerationRequest("anything")).Should().BeTrue();
    }

    [Fact]
    public void AllowRetryOnCollision_is_true()
    {
        _generator.AllowRetryOnCollision.Should().BeTrue();
    }
}
