using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// A set of questions that can be used to create a game.
/// </summary>
[Table("question_sets"), PrimaryKey(nameof(Id), nameof(Revision))]
public sealed class QuestionSet
{
    public const int MaxNameLength = 48;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required int Id { get; set; }

    public required int Revision { get; set; }

    [StringLength(MaxNameLength)]
    public required string Name { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }
    public required ApplicationUser User { get; set; }

    public required IList<Question> Questions { get; set; }

    public required CodeLanguage Language { get; set; }

    public required QuestionSet? CreatedFrom { get; set; }
}
