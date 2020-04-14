// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.IO;
using Stride.Core.Translation.Annotations;

namespace Stride.Core.Translation.Extractor
{
    internal class CSharpExtractor : ExtractorBase
    {
        // note: see https://stackoverflow.com/questions/4953737/regex-for-matching-c-sharp-string-literals
        private const string CSharpStringPattern = @"
            (                   # capturing group for the string
                @""             # verbatim string - match literal at-sign and a quote
                (?:
                    [^""]|""""  # match a non-quote character, or two quotes
                )*              # zero times or more
                ""              # literal quote
            |                   # OR - regular string
                ""              # string literal - opening quote
                (?:
                    \\.         # match an escaped character,
                    |[^\\""]    # or a character that isn't a quote or a backslash
                )*              # a few times
                ""              # string literal - closing quote
            )";
        private const RegexOptions PatternOptions = RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;

        private readonly (string, Regex)[] patterns =
        {
            (nameof(ITranslationProvider.GetString),                 new Regex($@"{nameof(ITranslationProvider.GetString)}\s*\(\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(ITranslationProvider.GetString),                 new Regex($@"{nameof(Tr._)}\s*\(\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(ITranslationProvider.GetParticularString),       new Regex($@"{nameof(ITranslationProvider.GetParticularString)}\s*\(\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(ITranslationProvider.GetParticularString),       new Regex($@"{nameof(Tr._p)}\s*\(\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(ITranslationProvider.GetPluralString),           new Regex($@"{nameof(ITranslationProvider.GetPluralString)}\s*\(\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(ITranslationProvider.GetPluralString),           new Regex($@"{nameof(Tr._n)}\s*\(\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(ITranslationProvider.GetParticularPluralString), new Regex($@"{nameof(ITranslationProvider.GetParticularPluralString)}\s*\(\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(ITranslationProvider.GetParticularPluralString), new Regex($@"{nameof(Tr._pn)}\s*\(\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}\s*,\s*{CSharpStringPattern}", PatternOptions)),
            (nameof(TranslationAttribute),                           new Regex($@"Translation\({CSharpStringPattern}(?:\s*,\s*{CSharpStringPattern})?(?:\s*,\s*{nameof(TranslationAttribute.Context)}\s*\=\s*{CSharpStringPattern})?", PatternOptions)),
        };

        public CSharpExtractor([NotNull] ICollection<UFile> inputFiles)
            : base(inputFiles, ".cs")
        {
        }

        protected override IEnumerable<Message> ExtractMessagesFromFile(UFile file)
        {
            var lineNumberLookup = new LineNumberLookup();
            // Read all content
            var content = File.ReadAllText(file.ToWindowsPath());
            // Remove comments
            content = RemoveComments(content);
            // Read content again to build the line number lookup
            using (var reader = new StringReader(content))
            {
                var stringBuilder = new StringBuilder();
                var lineNumber = 1L;
                var currentLine = reader.ReadLine();
                while (currentLine != null)
                {
                    stringBuilder.AppendLine(currentLine);
                    lineNumberLookup.Add((stringBuilder.Length, lineNumber++));
                    currentLine = reader.ReadLine();
                }
            }
            // Extract messages from content
            foreach (var (kind, pattern) in patterns)
            {
                foreach (var message in ExtractPattern(content, kind, pattern, lineNumberLookup))
                {
                    message.Source = file;
                    yield return message;
                }
            }

            string RemoveComments(string str)
            {
                const string blockPattern = @"
                    /\*                             # match the beginning of a block comment
                        (                           # capturing group for an intermediate line
                            [^\r\n]*?\r?\n          # match a line inside the block
                        )*?                         # zero times or more
                        (                           # capturing group for the last line closing the block
                            [^\r\n]*?\*/(?:\r?\n|$) # match last line in block comment
                        ){1}                        # exactly once
                    ";
                const string linePattern = @"//(.*?)(\r?\n|$)";

                // Note: we need to match literal strings to exclude false positives: they might contain comment characters.
                return Regex.Replace(str, $"{blockPattern}|{linePattern}|{CSharpStringPattern}", m =>
                    {
                        if (m.Value.StartsWith("/*") || m.Value.StartsWith("//"))
                        {
                            if (m.Value.StartsWith("//"))
                                return Environment.NewLine;
                            var count = m.Groups[1].Captures.Count + 1;
                            return string.Concat(Enumerable.Repeat(Environment.NewLine, count));
                        }
                        // Keep the literal strings
                        return m.Value;
                    },
                    RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            }
        }

        [ItemNotNull]
        private IEnumerable<Message> ExtractPattern([NotNull] string content, string kind, [NotNull] Regex pattern, [NotNull] LineNumberLookup lineNumberLookup)
        {
            foreach (Match match in pattern.Matches(content))
            {
                var groups = match.Groups;
                var message = new Message();
                switch (kind)
                {
                    case nameof(ITranslationProvider.GetString):
                        if (groups.Count < 2)
                            // TODO: report error
                            continue;
                        message.Text = Unescape(groups[1].Value);
                        break;

                    case nameof(ITranslationProvider.GetParticularString):
                        if (groups.Count < 3)
                            // TODO: report error
                            continue;
                        message.Context = Unescape(groups[1].Value);
                        message.Text = Unescape(groups[2].Value);

                        break;

                    case nameof(ITranslationProvider.GetPluralString):
                        if (groups.Count < 3)
                            // TODO: report error
                            continue;
                        message.Text = Unescape(groups[1].Value);
                        message.PluralText = Unescape(groups[2].Value);
                        break;

                    case nameof(ITranslationProvider.GetParticularPluralString):
                        if (groups.Count < 4)
                            // TODO: report error
                            continue;
                        message.Context = Unescape(groups[1].Value);
                        message.Text = Unescape(groups[2].Value);
                        message.PluralText = Unescape(groups[3].Value);
                        break;

                    case nameof(TranslationAttribute):
                        if (groups.Count < 3)
                            // TODO: report error
                            continue;
                        message.Text = Unescape(groups[1].Value);
                        if (groups[2].Success)
                            message.PluralText = Unescape(groups[2].Value);
                        if (groups[3].Success)
                            message.Context = Unescape(groups[3].Value);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
                }
                message.LineNumber = GetLineNumber(match);
                yield return message;
            }

            long GetLineNumber(Capture match)
            {
                var offset = lineNumberLookup.BinarySearch(match.Index);
                return offset >= 0 ? lineNumberLookup[offset].line : lineNumberLookup[~offset].line;
            }

            string Unescape(string str)
            {
                return str.StartsWith("@") ? str.Trim(@"@""".ToCharArray()) : Regex.Unescape(str.Trim(@"@""".ToCharArray()));
            }
        }

        private class LineNumberLookup : KeyedSortedList<long, (long position, long line)>
        {
            /// <inheritdoc />
            protected override long GetKeyForItem((long position, long line) item)
            {
                return item.position;
            }
        }
    }
}
