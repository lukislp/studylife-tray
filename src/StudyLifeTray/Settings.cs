using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StudyLifeTray;

/// <summary>
/// Persisted locally under %APPDATA%\StudyLifeTray\settings.json - just the server URL and the
/// TrayApiKey (obtained via the browser-consent connect flow, see ConnectFlow.cs), the key encrypted
/// with Windows DPAPI for the current user (see SettingsStore). No API key
/// is ever entered manually; the only thing a user provides directly is the server URL, same
/// "base URL only" contract every other studylife-* integration uses.
/// </summary>
public sealed record TrayAppSettings(string ServerUrl, string? ApiKey)
{
    public bool IsConnected => !string.IsNullOrEmpty(ApiKey);
}

public static class SettingsStore
{
    private static readonly string DefaultFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudyLifeTray", "settings.json");

    /// <summary>On-disk shape. The API key never touches the disk in plain text: it is written as
    /// <see cref="ApiKeyProtected"/> (Windows DPAPI, current-user scope, base64), so another Windows
    /// user, a backup of the file, or a copy of the folder cannot read it. <see cref="ApiKey"/> is
    /// only read, for files written by versions before this change, and migrated on first load.</summary>
    private sealed record StoredSettings(string ServerUrl, string? ApiKey = null, string? ApiKeyProtected = null);

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static TrayAppSettings Load() => Load(DefaultFilePath);

    public static void Save(TrayAppSettings settings) => Save(settings, DefaultFilePath);

    internal static TrayAppSettings Load(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return new TrayAppSettings("", null);
            var stored = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(filePath));
            if (stored is null) return new TrayAppSettings("", null);

            if (!string.IsNullOrEmpty(stored.ApiKeyProtected))
            {
                return new TrayAppSettings(stored.ServerUrl, Unprotect(stored.ApiKeyProtected));
            }

            var loaded = new TrayAppSettings(stored.ServerUrl, stored.ApiKey);
            if (loaded.IsConnected) TryMigrateToProtected(loaded, filePath);
            return loaded;
        }
        catch
        {
            // A corrupted or unreadable settings file (or a key sealed for another Windows user)
            // must not crash the app on startup - treat it exactly like "never configured" and
            // let the user reconnect.
            return new TrayAppSettings("", null);
        }
    }

    internal static void Save(TrayAppSettings settings, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);
        var stored = new StoredSettings(
            settings.ServerUrl,
            ApiKeyProtected: string.IsNullOrEmpty(settings.ApiKey) ? null : Protect(settings.ApiKey));
        File.WriteAllText(filePath, JsonSerializer.Serialize(stored, WriteOptions));
    }

    private static void TryMigrateToProtected(TrayAppSettings settings, string filePath)
    {
        try { Save(settings, filePath); }
        catch { /* best effort - the plain-text file stays readable and is retried on the next start */ }
    }

    private static string Protect(string apiKey) => Convert.ToBase64String(
        ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey), null, DataProtectionScope.CurrentUser));

    private static string Unprotect(string protectedKey) => Encoding.UTF8.GetString(
        ProtectedData.Unprotect(Convert.FromBase64String(protectedKey), null, DataProtectionScope.CurrentUser));
    /// <summary>Strips a trailing slash and any path/query/hash the user might have pasted in -
    /// only the origin is ever kept, mirroring every other studylife-* client's
    /// normalizeServerUrl.</summary>
    public static string NormalizeServerUrl(string raw)
    {
        var trimmed = raw.Trim().TrimEnd('/');
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : trimmed;
    }
}
