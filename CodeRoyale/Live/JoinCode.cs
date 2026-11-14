using System.Security.Cryptography;

namespace CodeRoyale.Live;

/// <summary>
/// Generates the short codes players type to join a game.
/// </summary>
public static class JoinCode
{
    /// <summary>
    /// 32 symbols with I, O, 1 and 0 removed, so a code can be read aloud across a
    /// classroom without anyone confusing a letter for a digit.
    /// </summary>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public const int Length = 6;

    /// <summary>Generates one code. Collisions are the caller's problem to retry.</summary>
    public static string Next()
    {
        char[] chars = new char[Length];
        for (int i = 0; i < Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(chars);
    }

    /// <summary>
    /// Normalises user input: trims, uppercases, and strips anything outside the alphabet
    /// so "abc-123" and "ABC123" reach the same game.
    /// </summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        return new string(input
            .ToUpperInvariant()
            .Where(c => Alphabet.Contains(c))
            .Take(Length)
            .ToArray());
    }

    public static bool IsWellFormed(string? input) => Normalize(input).Length == Length;
}
