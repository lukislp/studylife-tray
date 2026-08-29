using StudyLifeTray;
using Xunit;

namespace StudyLifeTray.Tests;

public class SettingsStoreTests
{
    [Theory]
    [InlineData("https://studylife.example.com/", "https://studylife.example.com")]
    [InlineData("https://studylife.example.com///", "https://studylife.example.com")]
    [InlineData("  https://studylife.example.com  ", "https://studylife.example.com")]
    [InlineData("https://studylife.example.com", "https://studylife.example.com")]
    [InlineData("https://studylife.example.com/setup", "https://studylife.example.com")]
    [InlineData("https://studylife.example.com/login?x=1#y", "https://studylife.example.com")]
    [InlineData("https://studylife.example.com:8443/anything", "https://studylife.example.com:8443")]
    public void NormalizeServerUrl_StripsToOrigin(string input, string expected)
    {
        Assert.Equal(expected, SettingsStore.NormalizeServerUrl(input));
    }

    [Fact]
    public void NormalizeServerUrl_LeavesAnUnparseableValueAsIs()
    {
        Assert.Equal("not a url", SettingsStore.NormalizeServerUrl("not a url"));
    }
}

public class TrayAppSettingsTests
{
    [Fact]
    public void IsConnected_TrueOnlyWithANonEmptyApiKey()
    {
        Assert.False(new TrayAppSettings("https://x", null).IsConnected);
        Assert.False(new TrayAppSettings("https://x", "").IsConnected);
        Assert.True(new TrayAppSettings("https://x", "secret").IsConnected);
    }
}
