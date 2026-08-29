using System.Text.Json;

namespace StudyLifeTray;

/// <summary>
/// Persisted locally under %APPDATA%\StudyLifeTray\settings.json - just the server URL and the
/// TrayApiKey (obtained via the browser-consent connect flow, see ConnectFlow.cs). No API key
/// is ever entered manually; the only thing a user provides directly is the server URL, same
/// "base URL only" contract every other studylife-* integration uses.
/// </summary>
public sealed record TrayAppSettings(string ServerUrl, string? ApiKey)
{
    public bool IsConnected => !string.IsNullOrEmpty(ApiKey);
}

public static class SettingsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudyLifeTray", "settings.json");

    public static TrayAppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new TrayAppSettings("", null);
            var json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<TrayAppSettings>(json);
            return loaded ?? new TrayAppSettings("", null);
        }
        catch
        {
            // A corrupted or unreadable settings file must not crash the app on startup - treat
            // it exactly like "never configured" and let the user reconnect.
            return new TrayAppSettings("", null);
        }
    }

    public static void Save(TrayAppSettings settings)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    /// <summary>Strips a trailing slash and any path/query/hash the user might have pasted in -
    /// only the origin is ever kept, mirroring every other studylife-* client's
    /// normalizeServerUrl.</summary>
    public static string NormalizeServerUrl(string raw)
    {
        var trimmed = raw.Trim().TrimEnd('/');
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : trimmed;
    }
}
