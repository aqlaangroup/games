using System.Security.Cryptography;

namespace Platform.Api.Admin;

/// <summary>
/// Authenticator-app codes (TOTP, RFC 6238: 6 digits, 30-second steps, HMAC-SHA1) for staff two-factor sign-in.
/// A code is accepted one step either side of now, and each step only once.
/// </summary>
public static class Totp
{
    const string Base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    const int StepSeconds = 30;

    public static string NewSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        var text = new System.Text.StringBuilder();
        for (int bit = 0; bit < bytes.Length * 8; bit += 5)
        {
            int value = 0;
            for (int i = 0; i < 5; i++)
            {
                int b = bit + i;
                value = (value << 1) | (b < bytes.Length * 8 ? (bytes[b / 8] >> (7 - b % 8)) & 1 : 0);
            }
            text.Append(Base32[value]);
        }
        return text.ToString();
    }

    static byte[] Decode(string secret)
    {
        var bits = new List<byte>();
        int buffer = 0, count = 0;
        foreach (char c in secret.TrimEnd('=').ToUpperInvariant())
        {
            int value = Base32.IndexOf(c);
            if (value < 0) throw new FormatException("Not base32.");
            buffer = (buffer << 5) | value;
            count += 5;
            if (count >= 8)
            {
                bits.Add((byte)(buffer >> (count - 8)));
                count -= 8;
            }
        }
        return bits.ToArray();
    }

    public static long Step(DateTimeOffset time) => time.ToUnixTimeSeconds() / StepSeconds;

    public static string Code(string secret, long step)
    {
        var counter = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        var hash = HMACSHA1.HashData(Decode(secret), counter);
        int offset = hash[^1] & 0x0F;
        int binary = (hash[offset] & 0x7F) << 24 | hash[offset + 1] << 16 | hash[offset + 2] << 8 | hash[offset + 3];
        return (binary % 1_000_000).ToString("000000");
    }

    /// <summary>The step the code matches, or null. lastStep: the last step used, which can't be used again.</summary>
    public static long? Match(string secret, string? code, DateTimeOffset now, long lastStep)
    {
        if (code is not { Length: 6 } || !code.All(char.IsAsciiDigit)) return null;
        long step = Step(now);
        for (long s = step - 1; s <= step + 1; s++)
            if (s > lastStep && CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Code(secret, s)), System.Text.Encoding.ASCII.GetBytes(code)))
                return s;
        return null;
    }

    public static string Uri(string secret, string email) =>
        $"otpauth://totp/Aqlaan%20Platform:{System.Uri.EscapeDataString(email)}?secret={secret}&issuer=Aqlaan%20Platform";
}
