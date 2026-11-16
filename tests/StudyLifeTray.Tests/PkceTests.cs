using System.Web;
using StudyLifeTray;
using Xunit;

namespace StudyLifeTray.Tests;

public class PkceTests
{
    [Fact]
    public void CreateChallenge_MatchesRfc7636AppendixBVector()
    {
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            Pkce.CreateChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
    }

    [Fact]
    public void CreateVerifier_IsUnpaddedBase64Url_AndFreshEachTime()
    {
        var a = Pkce.CreateVerifier();
        var b = Pkce.CreateVerifier();

        Assert.Equal(43, a.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BuildAuthUrl_CarriesChallengeAndS256Method()
    {
        var url = ConnectFlow.BuildAuthUrl("https://study.test", "http://127.0.0.1:51823/callback", "st", "the-challenge");

        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        Assert.Equal("/connect/tray", uri.AbsolutePath);
        Assert.Equal("http://127.0.0.1:51823/callback", query["redirect_uri"]);
        Assert.Equal("st", query["state"]);
        Assert.Equal("the-challenge", query["code_challenge"]);
        Assert.Equal("S256", query["code_challenge_method"]);
    }
}
