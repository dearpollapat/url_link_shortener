using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class CustomAliasGeneratorTests
{
    private readonly CustomAliasGenerator _generator = new();

    [Fact]
    public void Generate_returns_the_trimmed_alias()
    {
        _generator.Generate(new GenerationRequest("  mylink  ")).Should().Be("mylink");
    }

    [Fact]
    public void CanHandle_only_when_alias_present()
    {
        _generator.CanHandle(new GenerationRequest("mylink")).Should().BeTrue();
        _generator.CanHandle(new GenerationRequest(null)).Should().BeFalse();
        _generator.CanHandle(new GenerationRequest("   ")).Should().BeFalse();
    }

    [Fact]
    public void AllowRetryOnCollision_is_false()
    {
        _generator.AllowRetryOnCollision.Should().BeFalse();
    }
}
