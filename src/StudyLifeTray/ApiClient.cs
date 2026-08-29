using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StudyLifeTray;

/// <summary>Wire shape of StudyLife.Shared.Dtos.TimerStateDto (GET /api/timerstate) - only the
/// field this app actually reads. Mirrors studylife-focusguard/studylife-focustunes's own
/// hand-mirrored TimerStateDtoPayload (same reasoning: fail loudly and visibly here rather than
/// silently misreading a server-side field rename, not worth a generated client for one field).</summary>
public sealed class TimerStateDtoPayload
{
    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }
}

public enum PollResultKind
{
    Ok,
    Offline,
    Unauthorized,
    Http,
    Network,
}

public sealed record PollResult(PollResultKind Kind, bool IsRunning = false);

/// <summary>Authenticates with the long-lived TrayApiKey (provisioned via ConnectFlow.cs) via the
/// server's unified X-Api-Key gate. The Tray slot's ApiKeyScopes entry allows exactly this one
/// endpoint (plus Auth.Whoami) - a leaked key can therefore only ever reveal "is a session
/// running right now", nothing else (see the studylife repo's ApiKeyScopes.Tray doc comment).</summary>
public sealed class ApiClient
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private readonly HttpClient _http;

    /// <summary>httpClient is injectable purely for tests (a stub HttpMessageHandler) - real
    /// callers just use the parameterless default.</summary>
    public ApiClient(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient { Timeout = RequestTimeout };
    }

    public async Task<PollResult> PollTimerStateAsync(string serverUrl, string apiKey, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{serverUrl}/api/timerstate");
            request.Headers.Add("X-Api-Key", apiKey);
            using var response = await _http.SendAsync(request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new PollResult(PollResultKind.Unauthorized);
            if (!response.IsSuccessStatusCode)
                return new PollResult(PollResultKind.Http);

            var body = await response.Content.ReadFromJsonAsync<TimerStateDtoPayload>(ct);
            return new PollResult(PollResultKind.Ok, body?.IsRunning ?? false);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new PollResult(PollResultKind.Network); // our own RequestTimeout, not caller cancellation
        }
        catch (HttpRequestException)
        {
            return new PollResult(PollResultKind.Offline);
        }
    }

    /// <summary>Server-to-server exchange of the single-use connect assertion for the real
    /// AuthUserId and the plaintext TrayApiKey - see ConnectFlow.RunAsync. Anonymous POST: the
    /// assertion itself is the credential.</summary>
    public async Task<(int UserId, string TrayApiKey)?> ExchangeAssertionAsync(string serverUrl, string assertion, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync($"{serverUrl}/api/auth/tray-assertion-exchange",
            new { assertion }, ct);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadFromJsonAsync<TrayAssertionExchangeResponse>(cancellationToken: ct);
        if (body?.TrayApiKey is not { Length: > 0 }) return null;
        return (body.UserId, body.TrayApiKey);
    }

    private sealed class TrayAssertionExchangeResponse
    {
        [JsonPropertyName("userId")] public int UserId { get; set; }
        [JsonPropertyName("trayApiKey")] public string? TrayApiKey { get; set; }
    }
}
