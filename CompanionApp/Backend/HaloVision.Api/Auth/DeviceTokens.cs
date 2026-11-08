using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace HaloVision.Api.Auth;

// Makes and checks the secret tokens headsets use to log in.
public static class DeviceTokens
{
    // A new secret token: 32 random bytes, written as URL-safe text (43 characters).
    // 32 bytes = 256 bits of randomness, far too many possibilities to ever guess.
    public static string NewToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
    }

    // The SHA-256 fingerprint of a token, as hex text. This is what we store in the database.
    // SHA-256 is one-way: you can't get the token back from the hash, so a leaked database
    // can't be used to log in as a headset. To check a token, we hash it and compare hashes.
    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
