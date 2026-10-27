using System.Security.Cryptography;
using System.Text;

namespace Iscuissatis.Integration.Tests;

/// <summary>
/// Gera códigos TOTP (RFC 6238) iguais aos do aplicativo autenticador,
/// para exercitar o fluxo real de 2FA nos testes.
/// </summary>
public static class Totp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Compute(string base32Key, DateTimeOffset? at = null, int digits = 6, int period = 30)
    {
        var counter = (long)((at ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / period);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(DecodeBase32(base32Key));
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;

        var binary = ((hash[offset] & 0x7F) << 24)
                     | (hash[offset + 1] << 16)
                     | (hash[offset + 2] << 8)
                     | hash[offset + 3];

        return (binary % (int)Math.Pow(10, digits)).ToString().PadLeft(digits, '0');
    }

    private static byte[] DecodeBase32(string input)
    {
        var normalized = input.Trim().TrimEnd('=').Replace(" ", string.Empty).ToUpperInvariant();
        var bytes = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in normalized)
        {
            var value = Base32Alphabet.IndexOf(character);
            if (value < 0)
            {
                throw new ArgumentException($"Caractere inválido em chave base32: {character}", nameof(input));
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bytes.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return bytes.ToArray();
    }
}
