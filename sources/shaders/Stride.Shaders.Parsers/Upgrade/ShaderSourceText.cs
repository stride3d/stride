// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text;
using System.Text.RegularExpressions;

namespace Stride.Shaders.Parsing.Upgrade;

/// <summary>
/// Raw text of a <c>.sdsl</c> file, with the places that are code (not comments or strings), and text edits applied together.
/// The upgrade steps edit the raw text, so comments, <c>#if</c> blocks and formatting stay as they are.
/// </summary>
internal sealed class ShaderSourceText
{
    private readonly List<(int Start, int Length, string Text)> edits = [];

    public ShaderSourceText(string text)
    {
        Text = text;
        IsCode = new bool[text.Length];
        for (int i = 0; i < text.Length;)
        {
            if (text.AsSpan(i).StartsWith("//"))
            {
                var end = text.IndexOf('\n', i);
                i = end < 0 ? text.Length : end;
            }
            else if (text.AsSpan(i).StartsWith("/*"))
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 2;
            }
            else if (text[i] == '"')
            {
                var j = i + 1;
                while (j < text.Length && text[j] != '"' && text[j] != '\n')
                    j += text[j] == '\\' ? 2 : 1;
                i = Math.Min(j + 1, text.Length);
            }
            else
            {
                IsCode[i++] = true;
            }
        }
    }

    public string Text { get; }

    public bool[] IsCode { get; }

    public bool HasEdits => edits.Count > 0;

    public void Replace(int start, int length, string text) => edits.Add((start, length, text));

    public void Insert(int position, string text) => edits.Add((position, 0, text));

    /// <summary>
    /// The text with the edits applied (edits at the same position keep their order).
    /// </summary>
    public string Apply()
    {
        var result = new StringBuilder(Text);
        foreach (var (start, length, text) in edits.Select((x, index) => (x, index)).OrderByDescending(x => x.x.Start).ThenByDescending(x => x.index).Select(x => x.x))
        {
            result.Remove(start, length);
            result.Insert(start, text);
        }
        return result.ToString();
    }

    /// <summary>
    /// Matches of <paramref name="pattern"/> that start in code.
    /// </summary>
    public IEnumerable<Match> CodeMatches(string pattern, int start = 0, int end = -1)
    {
        end = end < 0 ? Text.Length : end;
        foreach (Match match in Regex.Matches(Text[..end], pattern))
        {
            if (match.Index >= start && IsCode[match.Index])
                yield return match;
        }
    }

    /// <summary>
    /// The position of the next code character <paramref name="c"/> from <paramref name="start"/>, or -1.
    /// </summary>
    public int NextCode(char c, int start, int end = -1)
    {
        end = end < 0 ? Text.Length : end;
        for (int i = start; i < end; i++)
        {
            if (IsCode[i] && Text[i] == c)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// The position of the brace closing the one at <paramref name="open"/>, or -1.
    /// </summary>
    public int MatchingBrace(int open)
    {
        var depth = 0;
        for (int i = open; i < Text.Length; i++)
        {
            if (!IsCode[i])
                continue;
            if (Text[i] == '{')
                depth++;
            else if (Text[i] == '}' && --depth == 0)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Brace depth at <paramref name="position"/>, counted from <paramref name="start"/>.
    /// </summary>
    public int Depth(int start, int position)
    {
        var depth = 0;
        for (int i = start; i < position; i++)
        {
            if (!IsCode[i])
                continue;
            if (Text[i] == '{')
                depth++;
            else if (Text[i] == '}')
                depth--;
        }
        return depth;
    }

    /// <summary>
    /// The declarations of the class <paramref name="name"/> (several when it is declared in each branch of an <c>#if</c>):
    /// <c>shader</c>, <c>class</c> or <c>module</c> keyword, the header after the name (generic parameters and parents), and the
    /// body braces.
    /// </summary>
    public IEnumerable<ShaderClassText> FindClass(string name)
    {
        foreach (var match in CodeMatches($@"(?<![\w.])(shader|class|module)\s+{Regex.Escape(name)}(?!\w)"))
        {
            var open = NextCode('{', match.Index + match.Length);
            var semicolon = NextCode(';', match.Index + match.Length);
            // A member like `module Foo x;` is not a declaration
            if (open < 0 || (semicolon >= 0 && semicolon < open))
                continue;
            var close = MatchingBrace(open);
            if (close < 0)
                continue;
            var keyword = match.Groups[1];
            yield return new ShaderClassText(keyword.Index, keyword.Value, match.Index + match.Length, open, close);
        }
    }

    /// <summary>
    /// The parents written in every class header of the text, including the branches of <c>#if</c> the parsed code doesn't have.
    /// </summary>
    public IEnumerable<string> AllParentNames()
    {
        foreach (var match in CodeMatches(@"(?<![\w.])(shader|class|module)\s+([A-Za-z_]\w*)"))
        {
            foreach (var classText in FindClass(match.Groups[2].Value).Where(x => x.Keyword == match.Groups[1].Index))
            {
                foreach (var parent in ParseParents(classText).Parents)
                    yield return parent.Name;
            }
        }
    }

    /// <summary>
    /// The parents written after <c>:</c> in a class header, with their places (generic arguments included).
    /// </summary>
    public (int Colon, List<(string Name, string Text, int Start, int End)> Parents) ParseParents(ShaderClassText shaderClass)
    {
        var parents = new List<(string, string, int, int)>();
        int depth = 0, colon = -1;
        for (int i = shaderClass.NameEnd; i < shaderClass.Open; i++)
        {
            if (!IsCode[i])
                continue;
            var c = Text[i];
            if (c is '<' or '(')
                depth++;
            else if (c is '>' or ')')
                depth--;
            else if (c == ':' && depth == 0)
            {
                colon = i;
                break;
            }
        }
        if (colon < 0)
            return (-1, parents);

        var start = colon + 1;
        depth = 0;
        for (int i = colon + 1; i <= shaderClass.Open; i++)
        {
            var c = Text[i];
            if (i < shaderClass.Open && !IsCode[i])
                continue;
            if (c is '<' or '(')
                depth++;
            else if (c is '>' or ')')
                depth--;
            else if ((c == ',' && depth == 0) || i == shaderClass.Open)
            {
                var text = Text[start..i];
                var trimmedStart = start + (text.Length - text.TrimStart().Length);
                var trimmed = text.Trim();
                if (trimmed.Length > 0)
                {
                    var genericStart = trimmed.IndexOf('<');
                    parents.Add(((genericStart < 0 ? trimmed : trimmed[..genericStart]).Trim(), trimmed, trimmedStart, trimmedStart + trimmed.Length));
                }
                start = i + 1;
            }
        }
        return (colon, parents);
    }

    /// <summary>
    /// The indentation of the body of the block opened at <paramref name="open"/>: its first line, or one level more than the
    /// line of the brace when the block is empty.
    /// </summary>
    public string BodyIndentation(int open)
    {
        var lineStart = Text.IndexOf('\n', open);
        while (lineStart >= 0 && lineStart + 1 < Text.Length)
        {
            var lineEnd = Text.IndexOf('\n', lineStart + 1);
            var line = Text[(lineStart + 1)..(lineEnd < 0 ? Text.Length : lineEnd)].TrimEnd('\r');
            if (line.Trim().Length > 0)
            {
                if (!line.TrimStart().StartsWith('}'))
                    return line[..(line.Length - line.TrimStart().Length)];
                break;
            }
            lineStart = lineEnd;
        }

        var braceLine = Text[(Text.LastIndexOf('\n', open) + 1)..open];
        var braceIndentation = braceLine[..(braceLine.Length - braceLine.TrimStart().Length)];
        return braceIndentation + (braceIndentation.Contains('\t') ? "\t" : "    ");
    }

    /// <summary>
    /// The line ending used by the file.
    /// </summary>
    public string NewLine => Text.Contains("\r\n") ? "\r\n" : "\n";
}

/// <param name="Keyword">Position of the <c>shader</c> / <c>class</c> / <c>module</c> keyword.</param>
/// <param name="NameEnd">End of the class name (the header follows).</param>
/// <param name="Open">The opening brace of the body.</param>
/// <param name="Close">The closing brace of the body.</param>
internal sealed record ShaderClassText(int Keyword, string KeywordText, int NameEnd, int Open, int Close);
