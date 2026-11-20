using CodeRoyale.Data.Questions;

namespace CodeRoyale.CodeRunner;

public class CodeEntryTestProcessor
{
    private readonly DateTime _answerTime;
    private readonly CodeEntryQuestion _question;
    private readonly string _code;

    internal CodeEntryTestProcessor(DateTime answerTime, CodeEntryQuestion question, string code)
    {
        _answerTime = answerTime;
        _question = question;
        _code = code;
    }

    public virtual async Task<CodeEntryTestResult> ProcessAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return new CodeEntryTestResult(_answerTime, [ ], _question.CorrectTests.ToList(), [ ], _question.IncorrectTests.ToList());
    }
}
