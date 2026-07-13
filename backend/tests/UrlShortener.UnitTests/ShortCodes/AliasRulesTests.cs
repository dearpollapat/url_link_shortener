using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class AliasRulesTests
{
    [Theory]
    [InlineData("mylink")]
    [InlineData("My-Link_1")]
    [InlineData("abc")]
    [InlineData("abcdefghijklmnopqrstuvwxyz1234")] // 30 chars
    public void IsWellFormed_accepts_valid_aliases(string alias)
    {
        AliasRules.IsWellFormed(alias).Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]                 // too short
    [InlineData("has space")]
    [InlineData("bad!char")]
    [InlineData("with.dot")]
    [InlineData("way-too-long-alias-that-exceeds-thirty-chars")]
    public void IsWellFormed_rejects_invalid_aliases(string alias)
    {
        AliasRules.IsWellFormed(alias).Should().BeFalse();
    }

    [Theory]
    [InlineData("api")]
    [InlineData("API")]      // case-insensitive
    [InlineData("openapi")]
    [InlineData("health")]
    public void IsReserved_flags_route_shadowing_names(string alias)
    {
        AliasRules.IsReserved(alias).Should().BeTrue();
    }

    [Theory]
    [InlineData("mylink")]
    [InlineData("gulf")]
    public void IsReserved_allows_normal_aliases(string alias)
    {
        AliasRules.IsReserved(alias).Should().BeFalse();
    }
}
