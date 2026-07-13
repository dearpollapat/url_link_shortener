using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.UnitTests.Services;

/// <summary>
/// These tests use mocks (NSubstitute) in place of the real collaborators to
/// prove the service is decoupled: it talks only to the abstractions
/// (ILinkRepository, IShortCodeGenerator, IUrlValidator) and its behavior is
/// driven entirely through those seams — never through a concrete type.
/// </summary>
public class LinkServiceDecouplingTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static LinkService Build(
        ILinkRepository repository,
        IShortCodeGenerator generator,
        IUrlValidator validator)
    {
        var resolver = new ShortCodeGeneratorResolver([generator]);
        var options = Options.Create(new ShortUrlOptions { BaseUrl = "http://localhost:5000" });
        return new LinkService(repository, resolver, validator, options, new FixedTimeProvider(Start));
    }

    private static IUrlValidator PassThroughValidator()
    {
        // Accepts anything, echoing the input back as the normalized URL.
        var validator = Substitute.For<IUrlValidator>();
        validator.TryNormalize(Arg.Any<string?>(), out Arg.Any<string>())
            .Returns(ci => { ci[1] = (string?)ci[0] ?? ""; return true; });
        return validator;
    }

    // --- Strategy seam: the service uses whatever generator it's given ---

    [Fact]
    public async Task Create_uses_the_code_produced_by_the_injected_generator()
    {
        // A generator the service has never heard of — proves code generation is
        // fully delegated through IShortCodeGenerator (Strategy / Open-Closed).
        var generator = Substitute.For<IShortCodeGenerator>();
        generator.CanHandle(Arg.Any<GenerationRequest>()).Returns(true);
        generator.AllowRetryOnCollision.Returns(true);
        generator.Generate(Arg.Any<GenerationRequest>()).Returns("PLUGIN1");

        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = Build(repo, generator, PassThroughValidator());

        var result = await service.CreateAsync(new CreateLinkRequest("https://example.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ShortCode.Should().Be("PLUGIN1");
        generator.Received(1).Generate(Arg.Any<GenerationRequest>());
    }

    // --- Repository seam: persistence goes through ILinkRepository ---

    [Fact]
    public async Task Create_reserves_the_code_through_the_repository()
    {
        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = Build(repo, new RandomShortCodeGenerator(), PassThroughValidator());

        await service.CreateAsync(new CreateLinkRequest("https://example.com"));

        await repo.Received(1).TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_retries_with_a_new_code_when_the_repository_reports_a_collision()
    {
        // Repository (not the service) owns uniqueness: it rejects twice, then
        // accepts. The random generator's retry policy drives the loop entirely
        // through the abstraction.
        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>())
            .Returns(false, false, true);

        var service = Build(repo, new RandomShortCodeGenerator(), PassThroughValidator());

        var result = await service.CreateAsync(new CreateLinkRequest("https://example.com"));

        result.IsSuccess.Should().BeTrue();
        await repo.Received(3).TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_does_not_retry_a_deterministic_generator_and_returns_conflict()
    {
        // Custom alias never changes, so a collision is final: exactly one
        // reservation attempt, then Conflict. Proves AllowRetryOnCollision is
        // what drives the decision — not a concrete generator check.
        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>()).Returns(false);

        var service = Build(repo, new CustomAliasGenerator(), PassThroughValidator());

        var result = await service.CreateAsync(new CreateLinkRequest("https://example.com", "mylink"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await repo.Received(1).TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redirect_writes_the_click_back_through_the_repository()
    {
        // Click-count persistence is a seam: after a visit the service must
        // hand the updated link back to the repository.
        var link = new Link("code01",
            new Dictionary<Platform, string> { [Platform.Default] = "https://example.com" }, Start);
        var repo = Substitute.For<ILinkRepository>();
        repo.GetAsync("code01", Arg.Any<CancellationToken>()).Returns(link);

        var service = Build(repo, new RandomShortCodeGenerator(), PassThroughValidator());

        var destination = await service.ResolveForRedirectAsync("code01", Platform.Default);

        destination.Should().Be("https://example.com");
        link.ClickCount.Should().Be(1);
        await repo.Received(1).UpdateAsync(link, Arg.Any<CancellationToken>());
    }

    // --- Validation seam: URL validation is delegated to IUrlValidator ---

    [Fact]
    public async Task Create_rejects_when_the_url_validator_rejects()
    {
        var validator = Substitute.For<IUrlValidator>();
        validator.TryNormalize(Arg.Any<string?>(), out Arg.Any<string>())
            .Returns(ci => { ci[1] = string.Empty; return false; });

        var repo = Substitute.For<ILinkRepository>();
        var service = Build(repo, new RandomShortCodeGenerator(), validator);

        var result = await service.CreateAsync(new CreateLinkRequest("anything-the-validator-says-no"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        // Validation failed before any storage was touched.
        await repo.DidNotReceive().TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }
}
