using System.Security.Cryptography;

namespace AutoBrowser.Helpers;

public static class UlidHelper
{
    private const string CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string NewUlid()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var chars = new char[26];

        // Encode timestamp (10 characters)
        var t = timestamp;
        for (var i = 9; i >= 0; i--)
        {
            chars[i] = CrockfordBase32[(int)(t % 32)];
            t /= 32;
        }

        // Encode 16 random bytes (16 characters)
        var randomBytes = RandomNumberGenerator.GetBytes(16);
        for (var i = 0; i < 16; i++)
        {
            chars[10 + i] = CrockfordBase32[randomBytes[i] % 32];
        }

        return new string(chars);
    }
}
