using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeRoyale.Data.Questions;

namespace CodeRoyale.Data.Games;

[Table("games_question_results")]
public class GamePlayerQuestionResult
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    /// <summary>
    /// The question being answered.
    /// </summary>
    public required Question Question { get; set; }

    /// <summary>
    /// Time it took them to answer.
    /// </summary>
    public required TimeSpan AnswerTime { get; set; }

    /// <summary>
    /// The player's response(s) to this question in string form.
    /// </summary>
    [StringLength(CodeEntryQuestion.MaxStartingTextLength)]
    public required string Response { get; set; }

    /// <summary>
    /// Points gained from answering this question
    /// </summary>
    public required int Score { get; set; }
}