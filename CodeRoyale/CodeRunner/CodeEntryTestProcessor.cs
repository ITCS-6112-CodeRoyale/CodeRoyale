using CodeRoyale.Data.Questions;

namespace CodeRoyale.CodeRunner;

public abstract class CodeEntryTestProcessor
{
    protected readonly DateTime AnswerTime;
    protected readonly CodeEntryQuestion Question;
    protected readonly string Code;

    internal CodeEntryTestProcessor(DateTime answerTime, CodeEntryQuestion question, string code)
    {
        AnswerTime = answerTime;
        Question = question;
        Code = code;
    }

    public abstract Task<CodeEntryTestResult> ProcessAsync(CancellationToken token);
}

/// <summary>
/// Fails all tests, used for unsupported languages.
/// </summary>
public sealed class NoOpCodeEntryTestProcessor : CodeEntryTestProcessor
{
    public NoOpCodeEntryTestProcessor(DateTime answerTime, CodeEntryQuestion question, string code)
        : base(answerTime, question, code) { }

    public override Task<CodeEntryTestResult> ProcessAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(new CodeEntryTestResult(AnswerTime, [ ], Question.CorrectTests.Select(x => new TestResult(x, null)).ToList(), [ ], Question.IncorrectTests.Select(x => new TestResult(x, null)).ToList(), null));
    }
}