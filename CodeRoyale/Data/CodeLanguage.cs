using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data;

/// <summary>
/// A supported programming language.
/// </summary>
[Table("language")]
public sealed class CodeLanguage
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)]
    public required string LanguageCode { get; set; }

    [StringLength(32)]
    public required string DisplayName { get; set; }
}