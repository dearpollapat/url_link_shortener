using UrlShortener.Api.Core;

namespace UrlShortener.UnitTests.PlatformDetection;

public class UserAgentPlatformDetectorTests
{
    private readonly UserAgentPlatformDetector _detector = new();

    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36")]
    [InlineData("Dalvik/2.1.0 (Linux; U; Android 13)")]
    public void Detect_returns_Android_for_android_agents(string userAgent)
    {
        _detector.Detect(userAgent).Should().Be(Platform.Android);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X)")]
    [InlineData("Mozilla/5.0 (iPod touch; CPU iPhone OS 15_0 like Mac OS X)")]
    public void Detect_returns_iOS_for_apple_mobile_agents(string userAgent)
    {
        _detector.Detect(userAgent).Should().Be(Platform.iOS);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)")]
    [InlineData("curl/8.4.0")]
    [InlineData("Googlebot/2.1")]
    public void Detect_falls_back_to_Default_for_unknown_or_missing_agents(string? userAgent)
    {
        _detector.Detect(userAgent).Should().Be(Platform.Default);
    }
}
