using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data.Questions;

[Table("questions")]
public abstract class Question
{
    public const int MaxTextLength = 1024;

    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Question text for the main prompt.
    /// </summary>
    [StringLength(MaxTextLength)]
    public required string Text { get; set; }
    
    /// <summary>
    /// Which type of question is this? This is the discriminator for EF.
    /// </summary>
    public abstract QuestionType Type { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }
}

public enum QuestionType
{
    /// <summary>
    /// Choose the correct answer from a list of answers.
    /// Implemented by <see cref="MultipleChoiceQuestion"/>.
    /// </summary>
    MultipleChoice,

    /// <summary>
    /// Enter a full code solution.
    /// Implemented by <see cref="CodeEntryQuestion"/>.
    /// </summary>
    CodeEntry,

    /// <summary>
    /// Fill in specific words in a question.
    /// Implemented by <see cref="BlankEntryQuestion"/>.
    /// </summary>
    BlankEntry
}