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
    public List<TestResult> PassedCorrectTests { get; set; }

    /// <summary>
    /// List of 'correct test's that failed.
    /// </summary>
    public List<TestResult> FailedCorrectTests { get; set; }

    /// <summary>
    /// List of 'incorrect test's that failed.
    /// </summary>
    public List<TestResult> FailedIncorrectTests { get; set; }

    /// <summary>
    /// List of 'incorrect test's that passed.
    /// </summary>
    public List<TestResult> PassedIncorrectTests { get; set; }

    /// <summary>
    /// Whether or not this test is correct.
    /// </summary>
    public bool IsCorrect => FailedCorrectTests.Count == 0 && PassedIncorrectTests.Count == 0 && string.IsNullOrEmpty(UserError);

    /// <summary>
    /// An error message or stack trace thrown by the user's code.
    /// </summary>
    public string? UserError { get; set; }

    internal CodeEntryTestResult(
        DateTime timestamp,
        List<TestResult> passedCorrectTests,
        List<TestResult> failedCorrectTests,
        List<TestResult> failedIncorrectTests,
        List<TestResult> passedIncorrectTests,
        string? userCodeError)
    {
        AnswerSubmittedTimestamp = timestamp;
        PassedCorrectTests = passedCorrectTests;
        FailedCorrectTests = failedCorrectTests;
        FailedIncorrectTests = failedIncorrectTests;
        PassedIncorrectTests = passedIncorrectTests;
        UserError = userCodeError;
    }
}

public sealed class TestResult
{
    public CodeEntryTest Test { get; }
    public string? Reason { get; }

    public TestResult(CodeEntryTest test, string? reason)
    {
        Test = test;
        Reason = reason;
    }
}