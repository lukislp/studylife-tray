using System.Diagnostics;
using System.Net;
using System.Web;

namespace StudyLifeTray;

public enum ConnectResultKind
{
    Success,
    InvalidServerUrl,
    Cancelled,
    Timeout,
    StateMismatch,
    ExchangeFailed,
}

public sealed record ConnectResult(ConnectResultKind Kind, string? ApiKey = null, string? Message = null);

/// <summary>
/// Browser-consent connect flow (identity contract v1 §2, audience "tray") - the one
/// non-browser-extension consumer of this flow. Every other studylife-* extension uses
/// chrome.identity.launchWebAuthFlow to catch its own redirect; a native desktop app has no
/// equivalent, so this runs a short-lived local HTTP listener on a loopback port instead (RFC
/// 8252 §7.3 "Loopback Interface Redirection") - AuthController.IsAllowedRedirectUri already
/// accepts exactly this (http://127.0.0.1:&lt;port&gt;/..., any port, any path).
///
/// Flow: bind a loopback listener on an OS-assigned port -&gt; open the system's default browser
/// to {serverUrl}/connect/tray?redirect_uri=...&amp;state=... (the user approves there, in a real
/// browser, using their existing StudyLife session) -&gt; the browser navigates to our loopback
/// listener with ?assertion=...&amp;state=... -&gt; verify state round-trips -&gt; exchange the
/// assertion server-to-server for the real TrayApiKey (ApiClient.ExchangeAssertionAsync).
/// </summary>
public static class ConnectFlow
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    public static async Task<ConnectResult> RunAsync(string rawServerUrl, ApiClient apiClient, CancellationToken ct)
    {
        var serverUrl = SettingsStore.NormalizeServerUrl(rawServerUrl);
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var parsed) || (parsed.Scheme != "http" && parsed.Scheme != "https"))
            return new ConnectResult(ConnectResultKind.InvalidServerUrl);

        using var listener = new HttpListener();
        // Port 0 would be ideal (OS-assigned), but HttpListener's prefix syntax requires an
        // explicit port - so instead this tries a small fixed range, starting from an uncommon,
        // unlikely-to-collide port, until one successfully binds.
        var port = 0;
        for (var candidate = 51823; candidate < 51833; candidate++)
        {
            try
            {
                listener.Prefixes.Clear();
                listener.Prefixes.Add($"http://127.0.0.1:{candidate}/");
                listener.Start();
                port = candidate;
                break;
            }
            catch (HttpListenerException)
            {
                // Port already in use - try the next candidate.
            }
        }
        if (port == 0) return new ConnectResult(ConnectResultKind.ExchangeFailed, Message: "No loopback port available.");

        var state = Guid.NewGuid().ToString("N");
        var redirectUri = $"http://127.0.0.1:{port}/callback";
        var authUrl = $"{serverUrl}/connect/tray?redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}";

        try
        {
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            return new ConnectResult(ConnectResultKind.ExchangeFailed, Message: $"Couldn't open the browser: {ex.Message}");
        }

        HttpListenerContext context;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(Timeout);
            var contextTask = listener.GetContextAsync();
            var completed = await Task.WhenAny(contextTask, Task.Delay(System.Threading.Timeout.Infinite, timeoutCts.Token));
            if (completed != contextTask) return new ConnectResult(ct.IsCancellationRequested ? ConnectResultKind.Cancelled : ConnectResultKind.Timeout);
            context = await contextTask;
        }
        catch (Exception)
        {
            return new ConnectResult(ConnectResultKind.Cancelled);
        }

        var query = HttpUtility.ParseQueryString(context.Request.Url!.Query);
        var assertion = query["assertion"];
        var returnedState = query["state"];

        await RespondWithClosePageAsync(context);
        listener.Stop();

        if (string.IsNullOrEmpty(assertion) || returnedState != state)
            return new ConnectResult(ConnectResultKind.StateMismatch);

        var exchange = await apiClient.ExchangeAssertionAsync(serverUrl, assertion, ct);
        if (exchange is null) return new ConnectResult(ConnectResultKind.ExchangeFailed);

        return new ConnectResult(ConnectResultKind.Success, exchange.Value.TrayApiKey);
    }

    private static async Task RespondWithClosePageAsync(HttpListenerContext context)
    {
        const string html = "<!doctype html><html><body style=\"font-family:sans-serif;text-align:center;padding-top:3rem\">" +
            "<h2>Connected to StudyLife</h2><p>You can close this window and return to the StudyLife Tray app.</p></body></html>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.OutputStream.Close();
    }
}
