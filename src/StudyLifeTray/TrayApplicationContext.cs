using System.Diagnostics;

namespace StudyLifeTray;

/// <summary>
/// The whole app's only UI surface: a NotifyIcon plus its context menu. No main window at all
/// (ApplicationContext, not Form, as the Application.Run root) - same shape as the browser
/// extensions' background service worker, just for a native desktop process instead.
///
/// Polls GET /api/timerstate every 30 seconds (System.Windows.Forms.Timer - the only persistent
/// timer primitive this needs, and unlike an MV3 service worker this process doesn't get killed
/// between ticks) and updates the tray icon/tooltip. No page-side "instant hint" path exists
/// here (that mechanism is browser-extension-specific - a native app has no page to listen to),
/// so 30s is the only cadence, not a fallback for something faster.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly ApiClient _apiClient = new();
    private TrayAppSettings _settings;
    private bool _polling;

    public TrayApplicationContext()
    {
        _settings = SettingsStore.Load();

        var openStudyLife = new ToolStripMenuItem("Open StudyLife", null, (_, _) => OpenStudyLife());
        var settingsItem = new ToolStripMenuItem("Settings…", null, (_, _) => OpenSettings());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread());
        var menu = new ContextMenuStrip();
        menu.Items.AddRange([openStudyLife, settingsItem, new ToolStripSeparator(), exitItem]);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application, // placeholder - swap for a real StudyLife-branded .ico
            Text = "StudyLife Tray",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => OpenStudyLife();

        _pollTimer = new System.Windows.Forms.Timer { Interval = (int)PollInterval.TotalMilliseconds };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _pollTimer.Start();

        UpdateIconForDisconnected();
        if (_settings.IsConnected) _ = PollAsync(); // pick up the current state immediately, not up to 30s later
        else OpenSettings(); // first run: nothing to show yet, go straight to setup
    }

    private void OpenStudyLife()
    {
        if (!_settings.IsConnected) { OpenSettings(); return; }
        Process.Start(new ProcessStartInfo(_settings.ServerUrl) { UseShellExecute = true });
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings, _apiClient);
        form.SettingsChanged += updated =>
        {
            _settings = updated;
            if (_settings.IsConnected) _ = PollAsync();
            else UpdateIconForDisconnected();
        };
        form.ShowDialog();
    }

    private async Task PollAsync()
    {
        // Single-flight: a slow poll must never overlap with the next timer tick piling up
        // concurrent requests against the same server.
        if (_polling || !_settings.IsConnected) return;
        _polling = true;
        try
        {
            var result = await _apiClient.PollTimerStateAsync(_settings.ServerUrl, _settings.ApiKey!, CancellationToken.None);
            switch (result.Kind)
            {
                case PollResultKind.Ok:
                    UpdateIconForSession(result.IsRunning);
                    break;
                case PollResultKind.Unauthorized:
                    // The key was revoked server-side - nothing this app can silently recover
                    // from, surface it plainly rather than polling a dead key forever.
                    _settings = new TrayAppSettings(_settings.ServerUrl, null);
                    SettingsStore.Save(_settings);
                    UpdateIconForDisconnected();
                    break;
                default:
                    // Offline/http/network failure: leave the last-known icon state alone - a
                    // transient hiccup must never suddenly flip the tray to "not running".
                    break;
            }
        }
        finally
        {
            _polling = false;
        }
    }

    private void UpdateIconForSession(bool isRunning)
    {
        _notifyIcon.Text = Truncate(isRunning ? "StudyLife: focus session active" : "StudyLife: no session running");
    }

    private void UpdateIconForDisconnected()
    {
        _notifyIcon.Text = "StudyLife: not connected";
    }

    // NotifyIcon.Text has a hard 63-character limit (throws ArgumentOutOfRangeException past it).
    private static string Truncate(string text) => text.Length <= 63 ? text : text[..63];

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Dispose();
            _notifyIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
