using CodeRoyale.Data;
using CodeRoyale.Data.Questions;

namespace CodeRoyale.CodeRunner;

public class CodeEntryTestProcessorFactory
{
    private readonly ILogger<CodeEntryTestProcessorFactory> _logger;

    public CodeEntryTestProcessorFactory(ILogger<CodeEntryTestProcessorFactory> logger)
    {
        _logger = logger;
    }

    public virtual CodeEntryTestProcessor CreateProcessor(DateTime answerTime, CodeEntryQuestion question, CodeLanguage language, string code)
    {
        return language.LanguageCode switch
        {
            "js" => new JS.JavaScriptCodeEntryTestProcessor(answerTime, question, code, _logger),
            _ => new NoOpCodeEntryTestProcessor(answerTime, question, code)
        };
    }
}
