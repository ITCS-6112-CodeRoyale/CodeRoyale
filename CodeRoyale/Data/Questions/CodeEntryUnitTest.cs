using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// Checks the validity of the user's code by inserting code into the scope of the question, which should throw an error if it fails.
/// </summary>
public class CodeEntryUnitTest : CodeEntryTest
{
    public const int MaxPreTestLength = 1024;
    public const int MaxPostTestLength = 8192;

    public override CodeEntryTestType Type
    {
        get => CodeEntryTestType.Regex;
        set { }
    }

    /// <summary>
    /// Code inserted before the user's code.
    /// </summary>
    [StringLength(MaxPreTestLength), DefaultValue(null)]
    public string? PreTest { get; set; }

    /// <summary>
    /// Code inserted after the user's code.
    /// </summary>
    [StringLength(MaxPostTestLength)]
    public required string PostTest { get; set; }
}