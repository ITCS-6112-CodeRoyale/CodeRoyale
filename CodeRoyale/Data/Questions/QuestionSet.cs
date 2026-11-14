using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// A set of questions that can be used to create a game.
/// </summary>
[Table("question_sets"), PrimaryKey(nameof(Id), nameof(Revision))]
public sealed class QuestionSet
{
    public const int MaxNameLength = 48;
    public const int MaxDescLength = 256;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required int Id { get; set; }

    public required int Revision { get; set; }

    [StringLength(MaxNameLength)]
    public required string Name { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    [ForeignKey(nameof(User))]
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public required IList<Question> Questions { get; set; }

    public required CodeLanguage Language { get; set; }

    public QuestionSet? CreatedFrom { get; set; }

    [StringLength(MaxDescLength)]
    public string? Description { get; set; }

    /* Game Options */
    public required double SecondsPerQuestion { get; set; }
}