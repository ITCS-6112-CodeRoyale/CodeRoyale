using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// Checks the user's submitted code against a regular expression.
/// </summary>
public class CodeEntryRegexTest : CodeEntryTest
{
    public const int MaxPatternLength = 512;

    internal static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(0.1d);

    public const RegexOptions DefaultRegexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.NonBacktracking;

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

    /// <summary>
    /// Options for regex parsing.
    /// </summary>
    [DefaultValue(DefaultRegexOptions)]
    public RegexOptions RegexFlags { get; set; } = DefaultRegexOptions;
}