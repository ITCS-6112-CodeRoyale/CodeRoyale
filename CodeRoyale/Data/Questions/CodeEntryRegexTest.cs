using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// Checks the user's submitted code against a regular expression.
/// </summary>
public class CodeEntryRegexTest : CodeEntryTest
{
    public const int MaxPatternLength = 512;

    public override CodeEntryTestType Type
    {
        get => CodeEntryTestType.Regex;
        set { }
    }

    /// <summary>
    /// The regular expression to match against the user's code.
    /// </summary>
    [StringLength(MaxPatternLength)]
    public required string Pattern { get; set; }

    /// <summary>
    /// Comments should be excluded from the regex check.
    /// </summary>
    [DefaultValue(true)]
    public bool ExcludeComments { get; set; } = true;

    /// <summary>
    /// Literals, such as strings, should be excluded from the regex check.
    /// </summary>
    [DefaultValue(true)]
    public bool ExcludeLiterals { get; set; } = true;
}