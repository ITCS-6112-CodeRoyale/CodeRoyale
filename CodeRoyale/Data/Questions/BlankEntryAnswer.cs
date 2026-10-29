using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// Expected value for a blank in a <see cref="BlankEntryQuestion"/>.
/// </summary>
[Table("questions_blank_answers")]
public class BlankEntryAnswer
{
    public const int MaxCorrectRegexLength = 1024;

    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 0-based index of which blank this answer is for. Blanks are represented by a {n} in <see cref="Question.Text"/>.
    /// </summary>
    public required byte Index { get; set; }

    /// <summary>
    /// A regular expression that determines whether or not an answer is correct.
    /// </summary>
    [StringLength(MaxCorrectRegexLength)]
    public required string CorrectRegex { get; set; }

    /// <summary>
    /// Options for regex parsing.
    /// </summary>
    [DefaultValue(RegexOptions.IgnoreCase)]
    public RegexOptions RegexFlags { get; set; } = RegexOptions.IgnoreCase;
}