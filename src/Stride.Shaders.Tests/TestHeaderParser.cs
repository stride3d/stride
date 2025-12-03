using System.Text.RegularExpressions;

namespace Stride.Shaders.Parsing.Tests;

// Note: generated with ChatGPT
public sealed class TestHeader
{
    public string Name { get; }
    public string Parameters { get; }

    public TestHeader(string name, string parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    public override string ToString() =>
        $"{Name}: {string.Join(", ", Parameters)}";
}

public static class TestHeaderParser
{
    // Matches: // TestName (Param1=..., Param2=..., ...)
    //    name  = "Test" in your example
    //    args  = "Param1=1, Param2=1, ExpectedResult=0x7F7F7F7F"
    private static readonly Regex HeaderRegex =
        new Regex(@"^\s*//\s*(?<name>[^(]+?)\s*\((?<args>.*)\)\s*$",
                  RegexOptions.Compiled);

    /// <summary>
    /// Parse all headers from the provided lines.
    /// </summary>
    public static IEnumerable<TestHeader> ParseHeaders(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            var m = HeaderRegex.Match(line);
            if (!m.Success) continue;

            var name = m.Groups["name"].Value.Trim();
            var args = m.Groups["args"].Value;

            var parameters = ParseParameters(args);
            yield return new TestHeader(name, args);
        }
    }

    /// <summary>
    /// Splits "A=1, B=foo, ExpectedResult=0xFF" into a dictionary.
    /// Supports quoted values with commas: A="hello, world".
    /// </summary>
    public static Dictionary<string, string> ParseParameters(string args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var piece in SplitArgs(args))
        {
            if (string.IsNullOrWhiteSpace(piece)) continue;

            var eqIndex = piece.IndexOf('=');
            if (eqIndex < 0)
            {
                // Parameter without value; store empty string
                var keyOnly = piece.Trim();
                if (!result.ContainsKey(keyOnly))
                    result[keyOnly] = string.Empty;
                continue;
            }

            var key = piece.Substring(0, eqIndex).Trim();
            var value = piece.Substring(eqIndex + 1).Trim();

            // Strip matching quotes if present
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Last-in wins on duplicate keys
            result[key] = value;
        }
        return result;
    }

    /// <summary>
    /// Splits by commas but ignores commas inside quotes.
    /// Accepts both single- and double-quoted values.
    /// </summary>
    private static IEnumerable<string> SplitArgs(string args)
    {
        if (string.IsNullOrEmpty(args))
            yield break;

        var current = new List<char>(args.Length);
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        int parenthesisLevel = 0;

        for (int i = 0; i < args.Length; i++)
        {
            char c = args[i];

            if (c == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                current.Add(c);
                continue;
            }

            if (c == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                current.Add(c);
                continue;
            }

            if (c == '(' && !inSingleQuote && !inDoubleQuote)
            {
                parenthesisLevel++;
                continue;
            }
            if (c == ')' && !inSingleQuote && !inDoubleQuote)
            {
                parenthesisLevel--;
                continue;
            }

            if (c == ',' && !inSingleQuote && !inDoubleQuote && parenthesisLevel == 0)
            {
                yield return new string(current.ToArray()).Trim();
                current.Clear();
                continue;
            }

            current.Add(c);
        }

        if (current.Count > 0)
            yield return new string(current.ToArray()).Trim();
    }
}