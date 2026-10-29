using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeRoyale.Data.Questions;

[Table("questions_code_tests")]
public abstract class CodeEntryTest
{
    public const int MaxDescriptionLength = 256;

    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Tests with the same value in this column will be considered to be a part of the same group of tests that will be evaluated as an 'OR' instead of an 'AND'.
    /// </summary>
    /// <remarks>Ignored if <c>0</c>.</remarks>
    [DefaultValue(0)]
    public int OrGroup { get; set; }

    /// <summary>
    /// Description shown to the user when the test fails.
    /// </summary>
    [StringLength(MaxDescriptionLength)]
    public required string Description { get; set; }

    /// <summary>
    /// Which type of test is this? This is the discriminator for EF.
    /// </summary>
    public abstract CodeEntryTestType Type { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }
}

public enum CodeEntryTestType
{
    Regex,

    Unit
}