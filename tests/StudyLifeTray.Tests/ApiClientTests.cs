using System.Net;
using StudyLifeTray;
using Xunit;

namespace StudyLifeTray.Tests;

/// <summary>ApiClient's HTTP-facing behavior against a stub handler that records requests -
/// mirrors the studylife repo's own AiProxyClientTests/WebhooksProxyClientTests style.</summary>
public class ApiClientTests
{
    [Fact]
    public async Task PollTimerStateAsync_SendsApiKeyHeader_AndParsesIsRunning()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"isRunning\":true}"),
        });
        var client = new ApiClient(new HttpClient(handler));

        var result = await client.PollTimerStateAsync("https://study.test", "secret-key", CancellationToken.None);

        Assert.Equal(PollResultKind.Ok, result.Kind);
        Assert.True(result.IsRunning);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://study.test/api/timerstate", request.Uri);
        Assert.Equal("secret-key", request.Headers["x-api-key"]);
    }

    [Fact]
    public async Task PollTimerStateAsync_Unauthorized_ReturnsUnauthorizedKind()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new ApiClient(new HttpClient(handler));

        var result = await client.PollTimerStateAsync("https://study.test", "revoked-key", CancellationToken.None);

        Assert.Equal(PollResultKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task PollTimerStateAsync_ServerError_ReturnsHttpKind()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new ApiClient(new HttpClient(handler));

        var result = await client.PollTimerStateAsync("https://study.test", "key", CancellationToken.None);

        Assert.Equal(PollResultKind.Http, result.Kind);
    }

    [Fact]
    public async Task PollTimerStateAsync_NetworkFailure_ReturnsOfflineKind()
    {
        var handler = new StubHttpHandler(_ => throw new HttpRequestException("connection refused"));
        var client = new ApiClient(new HttpClient(handler));

        var result = await client.PollTimerStateAsync("https://study.test", "key", CancellationToken.None);

        Assert.Equal(PollResultKind.Offline, result.Kind);
    }

    [Fact]
    public async Task ExchangeAssertionAsync_SendsAssertion_ReturnsUserIdAndKey()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"userId\":1,\"trayApiKey\":\"plaintext-key\"}"),
        });
        var client = new ApiClient(new HttpClient(handler));

        var result = await client.ExchangeAssertionAsync("https://study.test", "the-assertion", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Value.UserId);
        Assert.Equal("plaintext-key", result.Value.TrayApiKey);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://study.test/api/auth/tray-assertion-exchange", request.Uri);
        Assert.Contains("\"assertion\":\"the-assertion\"", request.Body);
    }

    [Fact]
    public async Task ExchangeAssertionAsync_UpstreamFailure_ReturnsNull()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new ApiClient(new HttpClient(handler));

        var result = await client.ExchangeAssertionAsync("https://study.test", "bad-assertion", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExchangeAssertionAsync_MissingKeyInResponse_ReturnsNull()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"userId\":1}"),
        });
        var client = new ApiClient(new HttpClient(handler));

        var result = await client.ExchangeAssertionAsync("https://study.test", "assertion", CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>Records all requests (including headers/body) and returns predefined responses -
    /// same deliberately plainly-built stub style as the studylife repo's own test suite.</summary>
    private sealed class StubHttpHandler : HttpMessageHandler
    {
        public sealed record RecordedRequest(string Uri, Dictionary<string, string> Headers, string Body);

        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<RecordedRequest> Requests { get; } = [];

        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = request.Headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => string.Join(",", h.Value));
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (Requests) Requests.Add(new RecordedRequest(request.RequestUri!.ToString(), headers, body));
            return _responder(request);
        }
    }
}
