using System.Security.Cryptography;
using System.Text;

namespace StudyLifeTray;

/// <summary>PKCE (RFC 7636, S256 only) helpers for the connect flow. A fresh verifier per attempt
/// keeps the assertion that arrives in the loopback redirect useless to any other local process.</summary>
public static class Pkce
{
    /// <summary>43-character code_verifier: base64url (no padding) of 32 CSPRNG bytes.</summary>
    public static string CreateVerifier() => Base64Url(RandomNumberGenerator.GetBytes(32));

    /// <summary>code_challenge = base64url_nopad(SHA-256(ASCII(verifier))).</summary>
    public static string CreateChallenge(string verifier) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
