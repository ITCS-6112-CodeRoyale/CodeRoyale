using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// An answer to a <see cref="MultipleChoiceQuestion"/>.
/// </summary>
[Table("question_mcq_answers")]
public class MultipleChoiceAnswer
{
    public const int MaxTextLength = 256;

    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 0-based index of this answer when the question isn't randomized.
    /// </summary>
    public required byte Index { get; set; }

    /// <summary>
    /// Answer text.
    /// </summary>
    [StringLength(MaxTextLength)]
    public required string Text { get; set; }

    /// <summary>
    /// Whether or not this answer is considered correct.
    /// More than one answer can be correct as long as the question is configured as <see cref="MultipleChoiceQuestion.MaxSelectCount"/> is greater than <c>1</c>.
    /// </summary>
    [DefaultValue(false)]
    public bool IsCorrect { get; set; }
}