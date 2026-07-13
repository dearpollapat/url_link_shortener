using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;

namespace UrlShortener.UnitTests.Persistence;

public class InMemoryLinkRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static Link BuildLink(string code = "abc123") =>
        new(code, new Dictionary<Platform, string> { [Platform.Default] = "https://a.com" }, Now);

    private readonly InMemoryLinkRepository _repo = new();

    [Fact]
    public async Task TryAdd_returns_false_on_duplicate_code()
    {
        (await _repo.TryAddAsync(BuildLink("dup"))).Should().BeTrue();
        (await _repo.TryAddAsync(BuildLink("dup"))).Should().BeFalse();
    }

    [Fact]
    public async Task TryAdd_is_case_insensitive()
    {
        (await _repo.TryAddAsync(BuildLink("MyLink"))).Should().BeTrue();
        (await _repo.TryAddAsync(BuildLink("mylink"))).Should().BeFalse();
    }

    [Fact]
    public async Task Update_after_delete_does_not_resurrect_the_link()
    {
        // Models the redirect race: a request reads the link and writes back its
        // click count (UpdateAsync) after a concurrent DeleteAsync removed it.
        var link = BuildLink();
        await _repo.TryAddAsync(link);
        await _repo.RemoveAsync(link.ShortCode);

        await _repo.UpdateAsync(link); // stale write-back from the in-flight visit

        (await _repo.GetAsync(link.ShortCode)).Should().BeNull();
    }

    [Fact]
    public async Task Concurrent_TryAdd_of_same_code_admits_exactly_one()
    {
        const int racers = 50;
        var links = Enumerable.Range(0, racers).Select(_ => BuildLink("race")).ToArray();

        var results = await Task.WhenAll(links.Select(l => _repo.TryAddAsync(l)));

        results.Count(won => won).Should().Be(1);
    }
}
