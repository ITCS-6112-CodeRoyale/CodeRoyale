using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data.Games;

[Table("game_players")]
public class GamePlayer
{
    public const int MaxNameLength = 32;

    [NotMapped]
    internal string? IntlGameId; // not GameId cause it messes up the automatically mapped property

    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    [ForeignKey(nameof(User))]
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [StringLength(MaxNameLength)]
    public required string Name { get; set; }

    public required DateTime JoinedTime { get; set; }

    [DefaultValue(null)]
    public DateTime? LeftTime { get; set; }

    [DefaultValue(0)]
    public double Score { get; set; }

#nullable disable

    public IList<GamePlayerQuestionResult> Results { get; set; }

#nullable disable
    [NotMapped]
    public ConcurrentDictionary<int, GamePlayerQuestionResult> Answers { get; } = new();
    public bool HasAnswered(int questionIndex) => Answers.ContainsKey(questionIndex);
}