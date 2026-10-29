using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeRoyale.Data.Questions;

public class CodeEntryQuestion : Question
{
    public const int MaxStartingTextLength = 4096;

    public override QuestionType Type
    {
        get => QuestionType.CodeEntry;
        set { }
    }

    /// <summary>
    /// Initial text in the code entry box.
    /// </summary>
    [DefaultValue(null), StringLength(MaxStartingTextLength)]
    public string? StartingText { get; set; }

#nullable disable

    /// <summary>
    /// List of tests that must pass for the answer to be considered correct.
    /// </summary>
    public IList<CodeEntryTest> CorrectTests { get; set; }

    /// <summary>
    /// List of tests that must fail for the answer to be considered correct.
    /// </summary>
    public IList<CodeEntryTest> IncorrectTests { get; set; }

#nullable restore
}