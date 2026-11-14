using CodeRoyale.Live;

namespace CodeRoyale.Tests;

public class JoinCodeTests
{
    [Fact]
    public void Codes_are_six_characters()
    {
        for (int i = 0; i < 200; i++)
        {
            Assert.Equal(JoinCode.Length, JoinCode.Next().Length);
        }
    }

    [Theory]
    [InlineData('I')]
    [InlineData('O')]
    [InlineData('0')]
    [InlineData('1')]
    public void Ambiguous_glyphs_never_appear(char forbidden)
    {
        // These are excluded so a code can be read aloud without being misheard.
        Assert.DoesNotContain(forbidden, JoinCode.Alphabet);

        for (int i = 0; i < 500; i++)
        {
            Assert.DoesNotContain(forbidden, JoinCode.Next());
        }
    }

    [Fact]
    public void Alphabet_has_no_duplicates()
    {
        Assert.Equal(JoinCode.Alphabet.Length, JoinCode.Alphabet.Distinct().Count());
    }

    [Fact]
    public void Codes_are_not_trivially_repeated()
    {
        // 32^6 is about a billion, so 500 draws colliding would mean the RNG is broken.
        HashSet<string> seen = new();
        for (int i = 0; i < 500; i++) seen.Add(JoinCode.Next());

        Assert.True(seen.Count > 495, $"only {seen.Count} distinct codes out of 500");
    }

    [Theory]
    [InlineData("abc234", "ABC234")]
    [InlineData("ABC-234", "ABC234")]
    [InlineData("  abc 234  ", "ABC234")]
    [InlineData("abc2341234", "ABC234")]   // excess trimmed to 6
    public void Normalise_accepts_sloppy_input(string input, string expected)
    {
        Assert.Equal(expected, JoinCode.Normalize(input));
    }

    [Fact]
    public void Normalise_strips_characters_outside_the_alphabet()
    {
        // I, O, 0 and 1 are not in the alphabet, so they are dropped rather than mapped.
        Assert.Equal("ABC", JoinCode.Normalize("AIBOC01"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC")]
    [InlineData("IIIIII")]
    public void IsWellFormed_rejects_anything_short_of_six_usable_characters(string? input)
    {
        Assert.False(JoinCode.IsWellFormed(input));
    }

    [Fact]
    public void IsWellFormed_accepts_a_generated_code()
    {
        Assert.True(JoinCode.IsWellFormed(JoinCode.Next()));
    }
}
