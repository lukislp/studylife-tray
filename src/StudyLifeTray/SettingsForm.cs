namespace StudyLifeTray;

/// <summary>Minimal, code-only settings window (no visual designer file - a handful of controls
/// doesn't warrant one) - server URL entry plus a Connect/Disconnect button. The actual
/// connect flow (ConnectFlow.RunAsync) runs from here, on the button's own click, mirroring
/// every other studylife-* client's "connect happens inside a real user gesture" shape.</summary>
public sealed class SettingsForm : Form
{
    private readonly TextBox _serverUrlBox;
    private readonly Label _statusLabel;
    private readonly Button _connectButton;
    private readonly ApiClient _apiClient;
    private TrayAppSettings _settings;

    public event Action<TrayAppSettings>? SettingsChanged;

    public SettingsForm(TrayAppSettings settings, ApiClient apiClient)
    {
        _settings = settings;
        _apiClient = apiClient;

        Text = "StudyLife Tray - Settings";
        Width = 420;
        Height = 200;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var urlLabel = new Label { Text = "StudyLife server URL:", Left = 16, Top = 16, Width = 380 };
        _serverUrlBox = new TextBox
        {
            Left = 16, Top = 40, Width = 380,
            Text = settings.ServerUrl,
            PlaceholderText = "https://studylife.example.com",
        };
        _statusLabel = new Label { Left = 16, Top = 72, Width = 380, Height = 40, Text = DescribeStatus(settings) };
        _connectButton = new Button { Left = 16, Top = 120, Width = 120, Text = settings.IsConnected ? "Reconnect" : "Connect" };
        _connectButton.Click += async (_, _) => await OnConnectClickedAsync();

        var disconnectButton = new Button { Left = 148, Top = 120, Width = 120, Text = "Disconnect", Enabled = settings.IsConnected };
        disconnectButton.Click += (_, _) =>
        {
            _settings = new TrayAppSettings(_settings.ServerUrl, null);
            SettingsStore.Save(_settings);
            SettingsChanged?.Invoke(_settings);
            _statusLabel.Text = DescribeStatus(_settings);
            _connectButton.Text = "Connect";
            disconnectButton.Enabled = false;
        };

        Controls.AddRange([urlLabel, _serverUrlBox, _statusLabel, _connectButton, disconnectButton]);
    }

    private static string DescribeStatus(TrayAppSettings settings) =>
        settings.IsConnected ? $"Connected to {settings.ServerUrl}" : "Not connected yet.";

    private async Task OnConnectClickedAsync()
    {
        _connectButton.Enabled = false;
        _statusLabel.Text = "Opening your browser to connect…";
        try
        {
            var result = await ConnectFlow.RunAsync(_serverUrlBox.Text, _apiClient, CancellationToken.None);
            if (result.Kind != ConnectResultKind.Success)
            {
                _statusLabel.Text = DescribeFailure(result);
                return;
            }

            var serverUrl = SettingsStore.NormalizeServerUrl(_serverUrlBox.Text);
            _settings = new TrayAppSettings(serverUrl, result.ApiKey);
            SettingsStore.Save(_settings);
            SettingsChanged?.Invoke(_settings);
            _statusLabel.Text = DescribeStatus(_settings);
            _connectButton.Text = "Reconnect";
        }
        finally
        {
            _connectButton.Enabled = true;
        }
    }

    private static string DescribeFailure(ConnectResult result) => result.Kind switch
    {
        ConnectResultKind.InvalidServerUrl => "Enter a valid server URL, e.g. https://studylife.example.com",
        ConnectResultKind.Cancelled => "Connection cancelled.",
        ConnectResultKind.Timeout => "Timed out waiting for the browser - try again.",
        ConnectResultKind.StateMismatch => "Couldn't verify the connection response - try again.",
        ConnectResultKind.ExchangeFailed => $"Couldn't complete the connection.{(result.Message is { Length: > 0 } m ? $" ({m})" : "")}",
        _ => "Connection failed.",
    };
}
