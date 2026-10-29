using CodeRoyale.Data.Questions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data.Games;

[Table("games")]
public class Game
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }
    public required ApplicationUser User { get; set; }

    public required QuestionSet QuestionSet { get; set; }

    public required DateTime StartedTime { get; set; }

    [DefaultValue(null)]
    public DateTime? CompletedTime { get; set; }

#nullable disable

    public IList<GamePlayer> Players { get; set; }

#nullable restore
}