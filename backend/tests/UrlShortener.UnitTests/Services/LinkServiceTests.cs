using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.UnitTests.Services;

/// <summary>
/// Behavior of the service against a real in-memory store — end-to-end for the
/// core user journey (create, stats, disable/enable, delete, redirect).
/// Decoupling from those collaborators is proven separately with mocks in
/// <see cref="LinkServiceDecouplingTests"/>.
/// </summary>
public class LinkServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);
    private const string ShortDomain = "http://localhost:5000";

    private readonly FixedTimeProvider _clock = new(Start);
    private readonly LinkService _service;

    public LinkServiceTests()
    {
        var resolver = new ShortCodeGeneratorResolver(
            [new CustomAliasGenerator(), new RandomShortCodeGenerator()]);
        var options = Options.Create(new ShortUrlOptions { BaseUrl = ShortDomain });
        _service = new LinkService(new InMemoryLinkRepository(), resolver, new UrlValidator(), options, _clock);
    }

    private async Task<Link> Create(CreateLinkRequest request)
    {
        var result = await _service.CreateAsync(request);
        result.IsSuccess.Should().BeTrue(because: result.IsFailure ? result.Error.Message : "");
        return result.Value;
    }

    // --- Create (happy path) ---

    [Fact]
    public async Task Create_returns_active_link_with_zero_clicks()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        link.Status.Should().Be(LinkStatus.Active);
        link.ClickCount.Should().Be(0);
        link.CreatedAt.Should().Be(Start);
        link.ShortCode.Should().HaveLength(7);
    }

    [Fact]
    public async Task Create_uses_custom_alias_when_supplied()
    {
        var link = await Create(new CreateLinkRequest("https://example.com", "mylink"));
        link.ShortCode.Should().Be("mylink");
    }

    [Fact]
    public async Task Create_stores_platform_destinations()
    {
        var link = await Create(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            }));

        link.ResolveDestination(Platform.Android).Should().Be("https://example.com/android.apk");
        link.ResolveDestination(Platform.iOS).Should().Be("https://example.com/app.ipa");
    }

    // --- Create (validation edge cases, returned as Result failures) ---

    [Theory]
    [InlineData("example.com")]            // no scheme
    [InlineData("javascript:alert(1)")]    // dangerous scheme
    [InlineData("ftp://x.com")]            // non-web scheme
    public async Task Create_rejects_invalid_url_scheme(string url)
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(url));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_rejects_link_pointing_back_to_short_domain()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest($"{ShortDomain}/somewhere"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("own domain");
    }

    [Fact]
    public async Task Create_rejects_platform_destination_pointing_back_to_short_domain()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string> { ["ios"] = $"{ShortDomain}/x" }));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_rejects_reserved_alias()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "api"));

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("reserved");
    }

    [Fact]
    public async Task Create_rejects_alias_with_special_characters()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "bad!alias"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_rejects_unknown_platform_key()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string> { ["windows"] = "https://example.com/win" }));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_with_duplicate_alias_returns_conflict()
    {
        await Create(new CreateLinkRequest("https://example.com", "dup"));

        var result = await _service.CreateAsync(new CreateLinkRequest("https://other.com", "dup"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Create_duplicate_alias_is_case_insensitive()
    {
        await Create(new CreateLinkRequest("https://example.com", "MyLink"));

        var result = await _service.CreateAsync(new CreateLinkRequest("https://other.com", "mylink"));

        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // --- Redirect / click counting ---

    [Fact]
    public async Task Redirect_increments_click_and_records_access_time()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));
        _clock.Advance(TimeSpan.FromMinutes(5));

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        destination.Should().StartWith("https://example.com");
        var reloaded = await _service.GetAsync(link.ShortCode);
        reloaded!.ClickCount.Should().Be(1);
        reloaded.LastAccessedAt.Should().Be(Start.AddMinutes(5));
    }

    [Fact]
    public async Task Redirect_selects_destination_by_platform()
    {
        var link = await Create(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            }));

        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Android))
            .Should().Be("https://example.com/android.apk");
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.iOS))
            .Should().Be("https://example.com/app.ipa");
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default))
            .Should().StartWith("https://example.com");
    }

    [Fact]
    public async Task Redirect_returns_null_for_missing_link()
    {
        (await _service.ResolveForRedirectAsync("nope", Platform.Default)).Should().BeNull();
    }

    [Fact]
    public async Task Redirect_on_disabled_link_returns_null_and_does_not_count()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));
        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        destination.Should().BeNull();
        (await _service.GetAsync(link.ShortCode))!.ClickCount.Should().Be(0);
    }

    // --- Disable / enable / delete ---

    [Fact]
    public async Task SetStatus_can_disable_then_re_enable()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default)).Should().BeNull();

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Active);
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default)).Should().NotBeNull();
    }

    [Fact]
    public async Task SetStatus_returns_not_found_for_missing_link()
    {
        var result = await _service.SetStatusAsync("nope", LinkStatus.Disabled);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Delete_removes_link_and_stops_redirects()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        var result = await _service.DeleteAsync(link.ShortCode);

        result.IsSuccess.Should().BeTrue();
        (await _service.GetAsync(link.ShortCode)).Should().BeNull();
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_returns_not_found_for_missing_link()
    {
        var result = await _service.DeleteAsync("nope");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Deleted_alias_can_be_reused()
    {
        var link = await Create(new CreateLinkRequest("https://example.com", "reuse"));
        await _service.DeleteAsync(link.ShortCode);

        var recreated = await Create(new CreateLinkRequest("https://other.com", "reuse"));
        recreated.ShortCode.Should().Be("reuse");
    }
}
