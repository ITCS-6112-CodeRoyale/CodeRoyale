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


    internal static string GetPlaceNumberWithSuffix(int num)
    {
        if (num < 0)
            return num.ToString();

        if (num <= 10)
        {
            return num switch
            {
                1 => "1st",
                2 => "2nd",
                3 => "3rd",
                _ => num + "th"
            };
        }

        int tens = num % 100;
        if (tens is > 10 and < 20)
        {
            return num + "th";
        }

        int ones = tens % 10;
        return ones switch
        {
            1 => num + "st",
            2 => num + "nd",
            3 => num + "rd",
            _ => num + "th"
        };
    }
}