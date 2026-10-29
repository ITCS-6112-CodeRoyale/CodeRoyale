namespace CodeRoyale.Data.Questions;

/// <summary>
/// Replaces values such as {0} with small text boxes that users have to type into.
/// </summary>
public class BlankEntryQuestion : Question
{
    public const int MaxAnswerCount = byte.MaxValue;

    public override QuestionType Type
    {
        get => QuestionType.BlankEntry;
        set { }
    }

#nullable disable

    /// <summary>
    /// List of answers that fill the blank spots in <see cref="Question.Text"/> designated with {n} placeholders.
    /// </summary>
    public IList<BlankEntryAnswer> Answers { get; set; }

#nullable restore
}