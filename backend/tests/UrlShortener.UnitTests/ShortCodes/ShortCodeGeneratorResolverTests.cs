using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class ShortCodeGeneratorResolverTests
{
    // Mirrors the DI registration order: custom alias first, random fallback last.
    private readonly ShortCodeGeneratorResolver _resolver = new(
        [new CustomAliasGenerator(), new RandomShortCodeGenerator()]);

    [Fact]
    public void Resolve_picks_custom_alias_when_alias_is_supplied()
    {
        _resolver.Resolve(new GenerationRequest("mylink")).Name.Should().Be("custom-alias");
    }

    [Fact]
    public void Resolve_falls_back_to_random_when_no_alias()
    {
        _resolver.Resolve(new GenerationRequest(null)).Name.Should().Be("random");
    }

    [Fact]
    public void Resolve_selects_a_newly_added_strategy_without_changing_the_resolver()
    {
        // A third strategy that claims requests whose alias starts with "vip-".
        // The resolver picks it purely via CanHandle — proving Open/Closed:
        // extending the set of generators needs no change to the resolver.
        var vip = Substitute.For<IShortCodeGenerator>();
        vip.Name.Returns("vip");
        vip.CanHandle(Arg.Is<GenerationRequest>(r => r.CustomAlias != null && r.CustomAlias.StartsWith("vip-")))
            .Returns(true);

        var resolver = new ShortCodeGeneratorResolver(
            [vip, new CustomAliasGenerator(), new RandomShortCodeGenerator()]);

        resolver.Resolve(new GenerationRequest("vip-gold")).Name.Should().Be("vip");
        resolver.Resolve(new GenerationRequest("normal")).Name.Should().Be("custom-alias");
    }

    [Fact]
    public void Constructor_throws_when_no_generators_registered()
    {
        var act = () => new ShortCodeGeneratorResolver([]);
        act.Should().Throw<InvalidOperationException>();
    }
}
