using System.ComponentModel;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// A question with multiple choices where one or more are correct.
/// </summary>
public class MultipleChoiceQuestion : Question
{
    public const int MaxAnswerCount = byte.MaxValue;

    public override QuestionType Type
    {
        get => QuestionType.MultipleChoice;
        set { }
    }

    /// <summary>
    /// Maximum number of values that can be chosen.
    /// </summary>
    [DefaultValue(1)]
    public int MaxSelectCount { get; set; } = 1;

    /// <summary>
    /// Minimum number of values that can be chosen.
    /// </summary>
    [DefaultValue(1)]
    public int MinSelectCount { get; set; } = 1;

    /// <summary>
    /// List of all possible answers to this question.
    /// </summary>
    public required IList<MultipleChoiceAnswer> Answers { get; set; }

    /// <summary>
    /// Randomize the answers for this question. If set overrides the game's setting.
    /// </summary>
    /// <remarks>This should usually be <see langword="false"/> for true and false questions so they stay in the same order.</remarks>
    public bool? RandomizeAnswers { get; set; }
}