using UrlShortener.Api.Core;

namespace UrlShortener.UnitTests;

public class LinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static Link BuildLink(params (Platform, string)[] destinations)
    {
        var map = destinations.ToDictionary(d => d.Item1, d => d.Item2);
        return new Link("abc123", map, Now);
    }

    [Fact]
    public void Constructor_requires_a_default_destination()
    {
        var withoutDefault = new Dictionary<Platform, string> { [Platform.iOS] = "https://a.io" };

        var act = () => new Link("abc", withoutDefault, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void New_link_is_active_with_zero_clicks_and_no_last_access()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));

        link.Status.Should().Be(LinkStatus.Active);
        link.ClickCount.Should().Be(0);
        link.LastAccessedAt.Should().BeNull();
    }

    [Fact]
    public void ResolveDestination_falls_back_to_default_for_unmapped_platform()
    {
        var link = BuildLink(
            (Platform.Default, "https://default.com"),
            (Platform.Android, "https://android.com"));

        link.ResolveDestination(Platform.Android).Should().Be("https://android.com");
        link.ResolveDestination(Platform.iOS).Should().Be("https://default.com");
    }

    [Fact]
    public void RegisterVisit_increments_count_and_records_time()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));

        link.RegisterVisit(Now);

        link.ClickCount.Should().Be(1);
        link.LastAccessedAt.Should().Be(Now);
    }

    [Fact]
    public void RegisterVisit_counts_are_atomic_under_concurrency()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));
        const int visits = 10_000;

        Parallel.For(0, visits, _ => link.RegisterVisit(Now));

        link.ClickCount.Should().Be(visits);
    }
}
