using System.Diagnostics;
using CodeRoyale.Data.Questions;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeRoyale.CodeRunner.JS;

/// <summary>
/// Uses Jint to simulate JavaScript code in-process.
/// </summary>
public sealed class JavaScriptCodeEntryTestProcessor : CodeEntryTestProcessor
{
    private readonly ILogger _logger;

    public JavaScriptCodeEntryTestProcessor(DateTime answerTime, CodeEntryQuestion question, string code, ILogger logger)
        : base(answerTime, question, code)
    {
        _logger = logger;
    }

    public override async Task<CodeEntryTestResult> ProcessAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        Stopwatch sw = Stopwatch.StartNew();

        UntrustedCodeLimits limits = UntrustedCodeLimits.Default with
        {
            TimeoutInterval = TimeSpan.FromSeconds(1),
            MaxStatements = 50_000,
            MemoryLimit = 16_000_000
        };

        StringBuilder finalCode = new StringBuilder(8192);

        Guid guid = Guid.NewGuid();

        // use a GUID so users cant reliably access the variable
        string stateVar = $"__codeRoyaleState_{guid:N}";

        // note: var adds this to the global scope which allows engine.GetValue to access it
        finalCode.Append("var ").Append(stateVar).AppendLine(
            " = { failedCorrectTests: [ ], failedIncorrectTests: [ ], failedCorrectPreTests: [ ], failedIncorrectPreTests: [ ] };"
        );

        bool hasHeader = false;

        foreach (CodeEntryUnitTest test in Question.CorrectTests.OfType<CodeEntryUnitTest>().OrderBy(x => x.Index))
        {
            if (string.IsNullOrEmpty(test.PreTest)) continue;
            if (!hasHeader)
            {
                finalCode.AppendLine("/* Correct Tests */");
                hasHeader = true;
            }
            finalCode.Append("/* Correct Test #").Append(test.Index + 1).AppendLine(" */");
            finalCode.Append(stateVar).Append(".testIndex = ").Append(test.Index).AppendLine(";");
            finalCode.AppendLine("try {");
            finalCode.AppendLine(test.PreTest);
            finalCode.AppendLine("} catch (err) {");
            finalCode.Append(stateVar).Append(".failedCorrectPreTests.push(").Append(test.Index).AppendLine(");");
            finalCode.AppendLine("}");
        }

        if (hasHeader)
            finalCode.AppendLine();

        hasHeader = false;
        foreach (CodeEntryUnitTest test in Question.IncorrectTests.OfType<CodeEntryUnitTest>().OrderBy(x => x.Index))
        {
            if (string.IsNullOrEmpty(test.PreTest)) continue;
            if (!hasHeader)
            {
                finalCode.AppendLine("/* Incorrect Tests */");
                hasHeader = true;
            }
            finalCode.Append("/* Incorrect Test #").Append(test.Index + 1).AppendLine(" */");
            finalCode.Append(stateVar).Append(".testIndex = ").Append(test.Index).AppendLine(";");
            finalCode.AppendLine("try {");
            finalCode.AppendLine(test.PreTest);
            finalCode.AppendLine("} catch (err) {");
            finalCode.Append(stateVar).Append(".failedIncorrectPreTests.push(").Append(test.Index).AppendLine(");");
            finalCode.AppendLine("}");
        }

        if (hasHeader)
            finalCode.AppendLine();

        finalCode.Append(stateVar).AppendLine(".testIndex = -1;");
        finalCode.AppendLine(Code);

        hasHeader = false;
        foreach (CodeEntryUnitTest test in Question.CorrectTests.OfType<CodeEntryUnitTest>().OrderBy(x => x.Index))
        {
            if (string.IsNullOrEmpty(test.PostTest)) continue;
            if (!hasHeader)
            {
                finalCode.AppendLine("/* Correct Tests */");
                hasHeader = true;
            }
            finalCode.Append("/* Correct Test #").Append(test.Index + 1).AppendLine(" */");
            finalCode.Append(stateVar).Append(".testIndex = ").Append(test.Index).AppendLine(";");
            finalCode.AppendLine("try {");
            finalCode.AppendLine(test.PostTest);
            finalCode.AppendLine("} catch (err) {");
            finalCode.Append(stateVar).Append(".failedCorrectTests.push(").Append(test.Index).AppendLine(");");
            finalCode.AppendLine("}");
        }

        if (hasHeader)
            finalCode.AppendLine();

        hasHeader = false;
        foreach (CodeEntryUnitTest test in Question.IncorrectTests.OfType<CodeEntryUnitTest>().OrderBy(x => x.Index))
        {
            if (string.IsNullOrEmpty(test.PostTest)) continue;
            if (!hasHeader)
            {
                finalCode.AppendLine("/* Incorrect Tests */");
                hasHeader = true;
            }

            finalCode.Append("/* Incorrect Test #").Append(test.Index + 1).AppendLine(" */");
            finalCode.Append(stateVar).Append(".testIndex = ").Append(test.Index).AppendLine(";");
            finalCode.AppendLine("try {");
            finalCode.AppendLine(test.PostTest);
            finalCode.Append(stateVar).Append(".failedIncorrectTests.push(").Append(test.Index).AppendLine(");");
            finalCode.AppendLine("} catch (err) { /* ignored */ }");
        }

        Options options = new Options().ForUntrustedCode(limits);
        options.Strict = true;

        using Engine engine = new Engine(options);

        string code = finalCode.ToString();

#if DEBUG
        _logger.LogDebug("Generated Code: " + Environment.NewLine + code);
#endif

        List<TestResult>
            failedCorrectTestsRes = new List<TestResult>(),
            failedIncorrectTestsRes = new List<TestResult>(),
            passedCorrectTestsRes = new List<TestResult>(),
            passedIncorrectTestsRes = new List<TestResult>();

        using (limits.BeginOperation(engine, token))
        {
            ObjectInstance? state = null;
            try
            {
                await engine.ExecuteAsync(code, Question.Id.ToString(), token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (JavaScriptException ex)
            {
                state = engine.GetValue(stateVar).AsObject();
                string userCodeError = ex.GetJavaScriptErrorString(ResultLimits.Conservative);
                return new CodeEntryTestResult(AnswerTime, [ ], [ ], [ ], [ ], userCodeError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trying to evaluate JS code.");
                return new CodeEntryTestResult(AnswerTime, [ ], [ ], [ ], [ ], ex.GetType().Name + " - " + ex.Message);
            }

            state ??= engine.GetValue(stateVar).AsObject();

            bool[] correctTests = new bool[Question.CorrectTests.Count];
            bool[] incorrectTests = new bool[Question.IncorrectTests.Count];

            JsArray failedCorrectTests = engine.GetValue(state, "failedCorrectTests").AsArray();
            JsArray failedIncorrectTests = engine.GetValue(state, "failedIncorrectTests").AsArray();

            JsArray failedCorrectPreTests = engine.GetValue(state, "failedCorrectPreTests").AsArray();
            JsArray failedIncorrectPreTests = engine.GetValue(state, "failedIncorrectPreTests").AsArray();

            foreach (JsValue value in failedCorrectPreTests)
            {
                int index = (int)Math.Round(value.AsNumber());
                CodeEntryTest? test = Question.CorrectTests.OfType<CodeEntryUnitTest>().FirstOrDefault(x => x.Index == index);
                if (test == null) continue;
                correctTests[index] = true;
                failedCorrectTestsRes.Add(new TestResult(test, null));
            }

            foreach (JsValue value in failedIncorrectPreTests)
            {
                int index = (int)Math.Round(value.AsNumber());
                CodeEntryTest? test = Question.IncorrectTests.OfType<CodeEntryUnitTest>().FirstOrDefault(x => x.Index == index);
                if (test == null) continue;
                incorrectTests[index] = true;
                passedIncorrectTestsRes.Add(new TestResult(test, null));
            }

            foreach (JsValue value in failedCorrectTests)
            {
                int index = (int)Math.Round(value.AsNumber());
                CodeEntryTest? test = Question.CorrectTests.OfType<CodeEntryUnitTest>().FirstOrDefault(x => x.Index == index);
                if (test == null) continue;
                correctTests[index] = true;
                failedCorrectTestsRes.Add(new TestResult(test, null));
            }

            foreach (JsValue value in failedIncorrectTests)
            {
                int index = (int)Math.Round(value.AsNumber());
                CodeEntryTest? test = Question.IncorrectTests.OfType<CodeEntryUnitTest>().FirstOrDefault(x => x.Index == index);
                if (test == null) continue;
                incorrectTests[index] = true;
                passedIncorrectTestsRes.Add(new TestResult(test, null));
            }

            _codeWithoutComments = new Lazy<string>(GetCodeWithoutComments, LazyThreadSafetyMode.None);
            _codeWithoutLiterals = new Lazy<string>(GetCodeWithoutLiterals, LazyThreadSafetyMode.None);
            _codeWithoutCommentsAndLiterals = new Lazy<string>(GetCodeWithoutCommentsAndLiterals, LazyThreadSafetyMode.None);

            foreach (CodeEntryRegexTest test in Question.CorrectTests.OfType<CodeEntryRegexTest>())
            {
                if (CheckRegexTest(test, out string? reason))
                {
                    passedCorrectTestsRes.Add(new TestResult(test, reason));
                }
                else
                {
                    failedCorrectTestsRes.Add(new TestResult(test, reason));
                }

                correctTests[test.Index] = true;
            }

            foreach (CodeEntryRegexTest test in Question.IncorrectTests.OfType<CodeEntryRegexTest>())
            {
                if (CheckRegexTest(test, out string? reason))
                {
                    passedIncorrectTestsRes.Add(new TestResult(test, reason));
                }
                else
                {
                    failedIncorrectTestsRes.Add(new TestResult(test, reason));
                }

                incorrectTests[test.Index] = true;
            }

            for (int i = 0; i < correctTests.Length; ++i)
            {
                if (correctTests[i])
                    continue;

                CodeEntryTest? test = Question.CorrectTests.FirstOrDefault(x => x.Index == i);
                if (test == null) continue;
                passedCorrectTestsRes.Add(new TestResult(test, null));
            }

            for (int i = 0; i < incorrectTests.Length; ++i)
            {
                if (incorrectTests[i])
                    continue;

                CodeEntryTest? test = Question.IncorrectTests.FirstOrDefault(x => x.Index == i);
                if (test == null) continue;
                failedIncorrectTestsRes.Add(new TestResult(test, null));
            }
        }

        sw.Stop();

        _logger.LogDebug("Tested code in {Elapsed:F2} ms.", sw.ElapsedTicks / (double)Stopwatch.Frequency * 1000d);

        return new CodeEntryTestResult(AnswerTime, passedCorrectTestsRes, failedCorrectTestsRes, failedIncorrectTestsRes, passedIncorrectTestsRes, null);
    }

    private bool CheckRegexTest(CodeEntryRegexTest test, out string? reason)
    {
        try
        {
            string code = Code;
            if (test.ExcludeLiterals)
            {
                code = test.ExcludeComments ? _codeWithoutCommentsAndLiterals.Value : _codeWithoutLiterals.Value;
            }
            else if (test.ExcludeComments)
            {
                code = _codeWithoutComments.Value;
            }

            Regex regex = new Regex(test.Pattern, test.RegexFlags & ~RegexOptions.Compiled, CodeEntryRegexTest.RegexTimeout);
            reason = null;
            return regex.IsMatch(code);
        }
        catch (RegexMatchTimeoutException)
        {
            _logger.LogWarning("RegEx timed out from pattern \"{p}\".", test.Pattern);
            reason = "Timed out while evaluating answer.";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RegEx error in pattern \"{p}\".", test.Pattern);
            reason = "Invalid pattern, notify question set creator.";
            return false;
        }
    }

#nullable disable

    private Lazy<string> _codeWithoutComments;
    private Lazy<string> _codeWithoutLiterals;
    private Lazy<string> _codeWithoutCommentsAndLiterals;

#nullable restore

    private string GetCodeWithoutComments()
    {
        return JavaScriptPreprocessingUtils.RemoveUserStringsFromCode(Code, true, false);
    }

    private string GetCodeWithoutLiterals()
    {
        return JavaScriptPreprocessingUtils.RemoveUserStringsFromCode(Code, false, true);
    }

    private string GetCodeWithoutCommentsAndLiterals()
    {
        return JavaScriptPreprocessingUtils.RemoveUserStringsFromCode(Code, true, true);
    }
}