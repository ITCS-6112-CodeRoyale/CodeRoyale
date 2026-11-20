using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace CodeRoyale.Data.Questions;

/// <summary>
/// Replaces values such as {0} with small text boxes that users have to type into.
/// </summary>
public class BlankEntryQuestion : Question
{
    private ImmutableArray<BlankEntryDataSpan> _spans;

    public const int MaxAnswerCount = byte.MaxValue;

    public override QuestionType Type
    {
        get => QuestionType.BlankEntry;
        set { }
    }

#nullable disable

    /// <summary>
    /// List of answers that fill the blank spots in <see cref="Question.Text"/> designated with {n} placeholders.
    /// </summary>
    public IList<BlankEntryAnswer> Answers { get; set; }

#nullable restore

    /// <summary>
    /// Splits the <see cref="Question.Text"/> up into sections.
    /// </summary>
    public ImmutableArray<BlankEntryDataSpan> GetSpans()
    {
        if (!_spans.IsDefault)
        {
            return _spans;
        }

        ImmutableArray<BlankEntryDataSpan>.Builder spans = ImmutableArray.CreateBuilder<BlankEntryDataSpan>(Answers.Count);

        string text = Text;
        int previousIndex = 0;
        for (int i = 0; i < text.Length; ++i)
        {
            char c = text[i];
            if (c != '{')
                continue;

            if (i == text.Length - 1 || text[i + 1] == '{')
            {
                ++i;
                continue;
            }

            int index = i + 1;
            while (index < text.Length && char.IsDigit(text[index]))
            {
                ++index;
            }

            if (index >= text.Length || text[index] != '}')
            {
                i = index - 1;
                continue;
            }

            if (!int.TryParse(text.AsSpan(i + 1, index - (i + 1)), CultureInfo.InvariantCulture, out int place) || place >= Answers.Count)
            {
                continue;
            }

            int lastIndex = i - 1;
            StringSegment prefix;
            if (lastIndex < previousIndex)
            {
                prefix = new StringSegment(string.Empty, 0, 0);
            }
            else
            {
                prefix = new StringSegment(Text, previousIndex, lastIndex - previousIndex + 1);
            }

            previousIndex = index + 1;

            spans.Add(new BlankEntryDataSpan { Prefix = prefix, AnswerIndex = place });
        }


        if (previousIndex < Text.Length)
        {
            spans.Add(new BlankEntryDataSpan { Prefix = new StringSegment(Text, previousIndex, Text.Length - previousIndex), AnswerIndex = -1 });
        }

        _spans = spans.DrainToImmutable();
        return _spans;
    }

    public MarkupString RecombineAnswers(IList<string?> answers)
    {
        ImmutableArray<BlankEntryDataSpan> spans = GetSpans();

        StringBuilder sb = new StringBuilder(Text.Length + Answers.Count * 16);
        StringWriter writer = new StringWriter(sb, CultureInfo.InvariantCulture);
        for (int i = 0; i < spans.Length; ++i)
        {
            BlankEntryDataSpan s = spans[i];
            sb.Append(s.Prefix.AsSpan());
            if (s.AnswerIndex < 0)
                continue;

            WebUtility.HtmlEncode(answers[s.AnswerIndex], writer);
        }

        writer.Flush();

        return new MarkupString(sb.ToString());
    }
}

public struct BlankEntryDataSpan
{
    public StringSegment Prefix;
    public int AnswerIndex; // or -1
}