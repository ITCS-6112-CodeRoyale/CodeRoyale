using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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

    public required int Index { get;set;}

    /// <summary>
    /// Question text for the main prompt.
    /// </summary>
    [StringLength(MaxTextLength)]
    public required string Text { get; set; }
    
    /// <summary>
    /// Which type of question is this? This is the discriminator for EF.
    /// </summary>
    public abstract QuestionType Type { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

    /// <summary>
    /// The amount of points a player gets by getting the question fully correct.
    /// </summary>
    [DefaultValue(1d)]
    public double Score { get; set; } = 1;

    /// <summary>
    /// Includes all answer lists for all question types.
    /// </summary>
    public static IQueryable<Question> IncludeAll(IQueryable<Question> q)
    {
        return q.Include(x => ((MultipleChoiceQuestion)x).Answers)
                .Include(x => ((CodeEntryQuestion)x).CorrectTests)
                .Include(x => ((CodeEntryQuestion)x).IncorrectTests)
                .Include(x => ((BlankEntryQuestion)x).Answers);
    }

    /// <summary>
    /// Includes all answer lists for all question types.
    /// </summary>
    public static IQueryable<QuestionSet> IncludeAll(IQueryable<QuestionSet> q)
    {
        return q.Include(x => x.Questions).ThenInclude(x => ((MultipleChoiceQuestion)x).Answers)
                .Include(x => x.Questions).ThenInclude(x => ((CodeEntryQuestion)x).CorrectTests)
                .Include(x => x.Questions).ThenInclude(x => ((CodeEntryQuestion)x).IncorrectTests)
                .Include(x => x.Questions).ThenInclude(x => ((BlankEntryQuestion)x).Answers);
    }
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