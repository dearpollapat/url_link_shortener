using UrlShortener.Api.Core.Validation;

namespace UrlShortener.UnitTests.Validation;

public class UrlValidatorTests
{
    private readonly UrlValidator _validator = new();

    [Theory]
    [InlineData("https://www.google.co.th")]
    [InlineData("http://example.com/path?q=1")]
    [InlineData("https://sub.domain.io:8443/a/b")]
    public void TryNormalize_accepts_absolute_http_urls(string url)
    {
        _validator.TryNormalize(url, out var normalized).Should().BeTrue();
        normalized.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("example.com")]                 // missing scheme -> not absolute
    [InlineData("/relative/path")]
    [InlineData("javascript:alert(1)")]          // dangerous scheme
    [InlineData("ftp://files.example.com")]      // non-web scheme
    [InlineData("file:///etc/passwd")]
    public void TryNormalize_rejects_invalid_or_unsafe_urls(string? url)
    {
        _validator.TryNormalize(url, out var normalized).Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Fact]
    public void TryNormalize_trims_whitespace()
    {
        _validator.TryNormalize("  https://example.com  ", out var normalized).Should().BeTrue();
        normalized.Should().StartWith("https://example.com");
    }
}
