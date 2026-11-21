using CodeRoyale.Data;
using CodeRoyale.Data.Questions;

namespace CodeRoyale.CodeRunner;

public class CodeEntryTestService
{
    private readonly ILogger<CodeEntryTestService> _logger;

    public CodeEntryTestService(ILogger<CodeEntryTestService> logger)
    {
        _logger = logger;
    }

    public CodeEntryTestProcessor CreateProcessor(DateTime answerTime, CodeEntryQuestion question, CodeLanguage language, string code)
    {
        return language.LanguageCode switch
        {
            "js" => new JS.JavaScriptCodeEntryTestProcessor(answerTime, question, code, _logger),
            _ => new NoOpCodeEntryTestProcessor(answerTime, question, code)
        };
    }
}
