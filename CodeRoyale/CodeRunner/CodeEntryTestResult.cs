using CodeRoyale.Data.Questions;

namespace CodeRoyale.CodeRunner;

/// <summary>
/// The result of checking the response to a code entry question.
/// </summary>
public sealed class CodeEntryTestResult
{
    /// <summary>
    /// Time when the answer was submitted.
    /// </summary>
    public DateTime AnswerSubmittedTimestamp { get; set; }

    /// <summary>
    /// List of 'correct test's that passed.
    /// </summary>
    public List<CodeEntryTest> PassedCorrectTests { get; set; }

    /// <summary>
    /// List of 'correct test's that failed.
    /// </summary>
    public List<CodeEntryTest> FailedCorrectTests { get; set; }

    /// <summary>
    /// List of 'incorrect test's that failed.
    /// </summary>
    public List<CodeEntryTest> FailedIncorrectTests { get; set; }

    /// <summary>
    /// List of 'incorrect test's that passed.
    /// </summary>
    public List<CodeEntryTest> PassedIncorrectTests { get; set; }

    /// <summary>
    /// Whether or not this test is correct.
    /// </summary>
    public bool IsCorrect => FailedCorrectTests.Count == 0 && PassedIncorrectTests.Count == 0;

    internal CodeEntryTestResult(DateTime timestamp, List<CodeEntryTest> passedCorrectTests, List<CodeEntryTest> failedCorrectTests, List<CodeEntryTest> failedIncorrectTests, List<CodeEntryTest> passedIncorrectTests)
    {
        AnswerSubmittedTimestamp = timestamp;
        PassedCorrectTests = passedCorrectTests;
        FailedCorrectTests = failedCorrectTests;
        FailedIncorrectTests = failedIncorrectTests;
        PassedIncorrectTests = passedIncorrectTests;
    }
}