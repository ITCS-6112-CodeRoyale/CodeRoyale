using System.Text;

namespace CodeRoyale.CodeRunner.JS;

internal static class JavaScriptPreprocessingUtils
{
    /// <summary>
    /// Optionally removes the contents of comments and string literals (including regex) from the code.
    /// </summary>
    /// <remarks>Strings are replaced by empty quotes, comments are fully removed.</remarks>
    internal static string RemoveUserStringsFromCode(string code, bool removeComments, bool removeLiterals)
    {
        if (!removeLiterals && !removeComments)
        {
            return code;
        }

        ReadOnlySpan<char> stops = [ '/', '"', '\'', '`' ];

        ReadOnlySpan<char> remaining = code.AsSpan();

        remaining.Trim();

        StringBuilder bldr = new StringBuilder(remaining.Length);

        if (remaining.StartsWith("#!", StringComparison.Ordinal))
        {
            int newLine = remaining.IndexOf('\n');
            if (removeComments)
            {
                if (newLine < 0)
                    return string.Empty;
            }
            else
            {
                bldr.Append(remaining[..newLine]);
            }

            remaining = remaining[newLine..];
        }

        while (!remaining.IsEmpty)
        {
            int index = remaining.IndexOfAny(stops);
            if (index < 0 || index >= remaining.Length - 1)
            {
                bldr.Append(remaining);
                break;
            }

            bldr.Append(remaining.Slice(0, index));
            remaining = remaining.Slice(index);

            char stop = remaining[0];
            switch (stop)
            {
                case '/':
                    int indexOfNextChar;
                    if (remaining[1] == '/')
                    {
                        // line comment
                        int endOfLine = remaining[2..].IndexOf('\n');
                        if (endOfLine < 0)
                            endOfLine = remaining.Length;
                        else
                            endOfLine += 2;

                        indexOfNextChar = Math.Min(endOfLine + 1, remaining.Length);
                    }
                    else if (remaining[1] == '*')
                    {
                        /* multiline comment */
                        int endOfComment = remaining[2..].IndexOf("*/");
                        if (endOfComment < 0)
                            endOfComment = remaining.Length;
                        else
                            endOfComment += 2;

                        indexOfNextChar = Math.Min(endOfComment + 2, remaining.Length);
                    }
                    else
                    {
                        // regexp
                        goto case '"';
                    }

                    if (!removeComments)
                    {
                        bldr.Append(remaining.Slice(0, indexOfNextChar));
                    }
                    else
                    {
                        int remove = 1;
                        while (bldr.Length >= remove && char.IsWhiteSpace(bldr[^remove]) && bldr[^remove] != '\n')
                        {
                            ++remove;
                        }

                        --remove;
                        if (remove > 0)
                        {
                            bldr.Remove(bldr.Length - remove, remove);
                        }
                    }

                    remaining = remaining.Slice(indexOfNextChar);
                    continue;

                case '"':
                case '\'':
                    // also regexp:
                    int endIndex = NextIndexOfWithEscapeSequences(remaining.Slice(1), stop);
                    if (endIndex < 0)
                        endIndex = remaining.Length;
                    else
                        ++endIndex;

                    if (stop == '/')
                    {
                        for (int i = endIndex + 1; i < remaining.Length && IsValidRegExpFlag(remaining[i]); ++i)
                        {
                            endIndex = i;
                        }
                    }

                    indexOfNextChar = Math.Min(endIndex + 1, remaining.Length);
                    if (!removeLiterals)
                    {
                        bldr.Append(remaining.Slice(0, indexOfNextChar));
                    }
                    else if (stop == '/')
                    {
                        // cant have an empty regex '//' since thats just a comment
                        bldr.Append("new RegExp(\"\")");
                    }
                    else
                    {
                        // append an empty string so the syntax still makes sense
                        bldr.Append(stop, 2);
                    }

                    remaining = remaining.Slice(indexOfNextChar);
                    continue;

                case '`':
                    remaining = remaining[1..];
                    bldr.Append('`');
                    int pos = RemoveTextFromInterpolatedString(remaining, bldr, !removeLiterals, removeComments);
                    remaining = remaining.Slice(pos + 1);
                    continue;
            }

            remaining = remaining[1..];
        }

        return bldr.ToString();
    }

    private static bool IsValidRegExpFlag(char c)
    {
        return c is 'd' or 'g' or 'i' or 'm' or 's' or 'u' or 'v' or 'y';
    }

    private static int NextIndexOfWithEscapeSequences(ReadOnlySpan<char> span, char c)
    {
        ReadOnlySpan<char> stops = [c, '\\'];

        bool isRegex = c == '/';

        int offset = 0;
        while (!span.IsEmpty)
        {
            int index = span.IndexOfAny(stops);
            if (index < 0)
                return -1;

            switch (span[index])
            {
                case '\\':

                    if (TryParseEscapeSequence(span.Slice(index), out int length, isRegex))
                    {
                        span = span.Slice(index + length);
                        offset += index + length;
                        continue;
                    }

                    break;

                case '\'':
                case '"':
                case '/':
                    return offset + index;
            }

            span = span.Slice(index + 1);
            offset += index + 1;
        }

        return -1;
    }

    private static bool TryParseEscapeSequence(ReadOnlySpan<char> span, out int length, bool isRegExp)
    {
        if (span.Length <= 1)
        {
            length = 1;
            return false;
        }

        switch (span[1])
        {
            case 'u' when span.Length >= 3:
                if (span[2] == '{')
                {
                    // \u{H..H}
                    int next = span.Slice(3).IndexOf('}');
                    if (next >= 0)
                    {
                        next += 3;
                        bool allAreHex = true;
                        for (int i = 3; i < next; ++i)
                        {
                            if (char.IsAsciiHexDigit(span[i]))
                            {
                                allAreHex = false;
                                break;
                            }
                        }

                        if (allAreHex)
                        {
                            length = next + 1;
                            return true;
                        }
                    }
                }
                else if (span.Length >= 6)
                {
                    // \uHHHH
                    bool allAreHex = true;
                    for (int i = 0; i < 4; ++i)
                    {
                        if (char.IsAsciiHexDigit(span[i + 2]))
                        {
                            allAreHex = false;
                            break;
                        }
                    }

                    if (allAreHex)
                    {
                        length = 6;
                        return true;
                    }
                }

                break;

            case 'x' when span.Length >= 4:
                // \xHH
                if (char.IsAsciiHexDigit(span[2]) && char.IsAsciiHexDigit(span[3]))
                {
                    length = 4;
                    return true;
                }
                break;

            case 'c' when isRegExp && span.Length >= 3:
                if (char.IsAsciiLetter(span[2]))
                {
                    // \cX
                    length = 3;
                    return true;
                }

                break;
        }

        // legacy octal sequences
        if (char.IsDigit(span[1]))
        {
            if (span.Length >= 3 && char.IsDigit(span[2]))
            {
                if (span.Length >= 4 && char.IsDigit(span[3]))
                {
                    // \000
                    length = 4;
                    return true;
                }

                // \00
                length = 3;
                return true;
            }
        }

        // \n, \0, etc
        length = 2;
        return true;
    }

    private static int RemoveTextFromInterpolatedString(ReadOnlySpan<char> span, StringBuilder bldr, bool append, bool removeComments)
    {
        // returns the index of the end quote in 'span'

        // span doesnt include the first `

        if (span.IsEmpty)
        {
            bldr.Append('`');
            return 0;
        }

        Span<int> depths = stackalloc int[4];

        ReadOnlySpan<char> stops = [ '"', '\'', '/', '$', '`', '[', ']', '(', ')', '{', '}', '<', '>', '\\' ];

        bool isInArg = false;

        int offset = 0;

        while (!span.IsEmpty)
        {
            int index = span.IndexOfAny(stops);
            if (index < 0)
                break;

            char c = span[index];
            switch (c)
            {
                case '\\':

                    if (TryParseEscapeSequence(span.Slice(index), out int length, false))
                    {
                        if (append)
                        {
                            bldr.Append(span.Slice(0, index + length));
                        }
                        span = span.Slice(index + length);
                        offset += index + length;
                        continue;
                    }

                    break;

                case '$' when index + 1 < span.Length && span[index + 1] == '{':
                    if (isInArg)
                        break;

                    if (append)
                    {
                        bldr.Append(span.Slice(0, index + 1));
                    }
                    else
                    {
                        bldr.Append('$');
                    }

                    span = span.Slice(index + 1);
                    offset += index + 1;
                    isInArg = true;
                    continue;

                case '{':
                case '[':
                case '(':
                case '<':
                    if (!isInArg)
                        break;

                    ++depths[DepthIndex(c)];
                    break;

                case '}':
                case ']':
                case ')':
                case '>':
                    if (!isInArg)
                        break;

                    ref int d = ref depths[DepthIndex(c)];
                    int depth = Math.Max(0, d - 1);
                    d = depth;
                    if (depth == 0 && c == '}')
                    {
                        isInArg = false;
                        if (!append)
                        {
                            bldr.Append(span.Slice(0, index + 1));
                        }
                    }
                    break;

                case '/':

                    if (index + 1 < span.Length)
                    {
                        int indexOfNextChar;
                        switch (span[index + 1])
                        {
                            case '/':
                                // line comment
                                if (append)
                                    bldr.Append(span.Slice(0, index));

                                span = span[index..];
                                offset += index;
                                int endOfLine = span.IndexOf('\n');
                                if (endOfLine < 0)
                                    endOfLine = span.Length;

                                indexOfNextChar = Math.Min(endOfLine + 1, span.Length);

                                if (!removeComments)
                                {
                                    bldr.Append(span.Slice(0, indexOfNextChar));
                                }

                                span = span[indexOfNextChar..];
                                offset += indexOfNextChar;
                                continue;

                            case '*':
                                /* multiline comment */
                                if (append)
                                    bldr.Append(span.Slice(0, index));

                                span = span[index..];
                                offset += index;
                                int endOfComment = span.IndexOf("*/");
                                if (endOfComment < 0)
                                    endOfComment = span.Length;

                                indexOfNextChar = Math.Min(endOfComment + 2, span.Length);

                                if (!removeComments)
                                {
                                    bldr.Append(span.Slice(0, indexOfNextChar));
                                }

                                span = span[indexOfNextChar..];
                                offset += indexOfNextChar;
                                continue;
                        }

                        goto case '"';
                    }
                    break;

                case '"':
                case '\'':
                    // case '/':
                    if (!isInArg)
                        break;

                    int endIndex = NextIndexOfWithEscapeSequences(span.Slice(index + 1), c);
                    if (endIndex < 0)
                        break;

                    endIndex += index + 1;

                    if (c == '/')
                    {
                        for (int i = endIndex + 1; i < span.Length && IsValidRegExpFlag(span[i]); ++i)
                        {
                            endIndex = i;
                        }
                    }

                    if (append)
                    {
                        bldr.Append(span.Slice(0, endIndex));
                    }
                    else if (c == '/')
                    {
                        bldr.Append("new RegExp(\"\")");
                    }
                    else
                    {
                        bldr.Append(c, 2);
                    }

                    int o = endIndex + 1;
                    span = span[o..];
                    offset += o;
                    continue;

                case '`':

                    if (append)
                    {
                        bldr.Append(span.Slice(0, index + 1));
                    }
                    else
                    {
                        bldr.Append('`');
                    }

                    if (!isInArg)
                    {
                        return offset + index;
                    }

                    span = span.Slice(index + 1);
                    offset += index + 1;
                    int pos = RemoveTextFromInterpolatedString(span, bldr, append, removeComments) + 1;
                    span = span.Slice(pos);
                    offset += pos;
                    continue;
            }

            if (append || isInArg)
            {
                bldr.Append(span.Slice(0, index + 1));
            }
            span = span.Slice(index + 1);
            offset += index + 1;
        }

        return offset;

        static int DepthIndex(char c)
        {
            switch (c)
            {
                case '[':
                case ']':
                    return 0;
                case '(':
                case ')':
                    return 1;
                case '{':
                case '}':
                    return 2;
                case '<':
                case '>':
                    return 3;
                default: return -1;
            }
        }
    }
}