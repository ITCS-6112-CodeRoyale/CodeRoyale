using CodeRoyale.Data.Questions;

namespace CodeRoyale.CodeRunner;

public class CodeEntryTestService
{
    public CodeEntryTestProcessor CreateProcessor(DateTime answerTime, CodeEntryQuestion question, string code)
    {
        return new CodeEntryTestProcessor(answerTime, question, code);
    }
}
