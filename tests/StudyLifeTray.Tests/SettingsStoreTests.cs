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

public class SettingsStorePersistenceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "studylife-tray-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Save_NeverWritesTheApiKeyInPlainText_AndLoadReturnsIt()
    {
        SettingsStore.Save(new TrayAppSettings("https://x", "super-secret-key"), FilePath);

        var onDisk = File.ReadAllText(FilePath);
        Assert.DoesNotContain("super-secret-key", onDisk);
        Assert.Contains("ApiKeyProtected", onDisk);
        Assert.Equal(new TrayAppSettings("https://x", "super-secret-key"), SettingsStore.Load(FilePath));
    }

    [Fact]
    public void Save_WithoutAKey_WritesNoKeyField()
    {
        SettingsStore.Save(new TrayAppSettings("https://x", null), FilePath);

        Assert.DoesNotContain("ApiKey", File.ReadAllText(FilePath));
        Assert.False(SettingsStore.Load(FilePath).IsConnected);
    }

    [Fact]
    public void Load_MigratesALegacyPlainTextFile()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, """{ "ServerUrl": "https://x", "ApiKey": "legacy-key" }""");

        Assert.Equal("legacy-key", SettingsStore.Load(FilePath).ApiKey);

        Assert.DoesNotContain("legacy-key", File.ReadAllText(FilePath));
        Assert.Equal("legacy-key", SettingsStore.Load(FilePath).ApiKey);
    }

    [Fact]
    public void Load_TreatsACorruptedProtectedKeyAsNotConfigured()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, """{ "ServerUrl": "https://x", "ApiKeyProtected": "AAAA" }""");

        Assert.False(SettingsStore.Load(FilePath).IsConnected);
    }
}