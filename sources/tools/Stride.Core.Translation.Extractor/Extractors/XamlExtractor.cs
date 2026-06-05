// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core.Translation.Extractor
{
    internal class XamlExtractor : ExtractorBase
    {
        // CLR/using: namespace URIs containing these strings host a Localize markup extension
        private static readonly string[] LocalizeClrNamespaceMarkers =
        [
            "Stride.Core.Translation.Presentation",    // WPF: Stride.Core.Translation.Presentation.Wpf.MarkupExtensions
            "Stride.Core.Presentation.Avalonia",       // Avalonia: MarkupExtensions.LocalizeStringExtension + Converters.Localize
        ];
        // Compact XML namespace URI shared between WPF and Avalonia Stride assemblies
        private const string StrideXamlPresentationNamespace = "http://schemas.stride3d.net/xaml/presentation";

        public XamlExtractor([NotNull] ICollection<UFile> inputFiles)
            : base(inputFiles, ".xaml", ".axaml")
        {
        }

        protected override IEnumerable<Message> ExtractMessagesFromFile(UFile file)
        {
            try
            {
                var doc = XDocument.Load(file.ToOSPath(), LoadOptions.SetLineInfo);
                return ExtractFromDocument(file, doc);
            }
            catch (XmlException ex)
            {
                Console.Error.WriteLine($"{file.ToOSPath()}: {ex.Message}");
                return [];
            }
        }

        private static IEnumerable<Message> ExtractFromDocument(UFile file, XDocument doc)
        {
            // Build prefix -> namespace URI map from all xmlns:prefix declarations in the document.
            // Namespace prefixes are author-controlled (could be "loc", "localize", etc.), so we
            // detect which ones map to a known translation namespace rather than matching by name.
            var prefixToNamespace = doc.Descendants()
                .SelectMany(e => e.Attributes())
                .Where(a => a.IsNamespaceDeclaration && a.Name.LocalName != "xmlns")
                .GroupBy(a => a.Name.LocalName)
                .ToDictionary(g => g.Key, g => g.First().Value);

            var localizePrefixes = prefixToNamespace
                .Where(kv => IsLocalizeNamespace(kv.Value))
                .Select(kv => kv.Key)
                .ToHashSet();

            var localizeNamespaces = prefixToNamespace
                .Where(kv => localizePrefixes.Contains(kv.Key))
                .Select(kv => kv.Value)
                .ToHashSet();

            if (localizePrefixes.Count == 0)
                yield break;

            foreach (var element in doc.Descendants())
            {
                var lineInfo = (IXmlLineInfo)element;
                int lineNumber = lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;

                // Element form: <prefix:Localize .../> or <prefix:LocalizeString .../>
                if (IsLocalizeLocalName(element.Name.LocalName) && localizeNamespaces.Contains(element.Name.NamespaceName))
                {
                    var msg = ExtractFromLocalizeElement(element);
                    if (!string.IsNullOrEmpty(msg.Text))
                    {
                        msg.Source = file;
                        msg.LineNumber = lineNumber;
                        yield return msg;
                    }
                    continue;
                }

                // Attribute form: SomeProp="{prefix:Localize ...}"
                foreach (var attr in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
                {
                    var attrLineInfo = (IXmlLineInfo)attr;
                    int attrLine = attrLineInfo.HasLineInfo() ? attrLineInfo.LineNumber : lineNumber;

                    foreach (var msg in ExtractFromAttributeValue(attr.Value, localizePrefixes))
                    {
                        if (!string.IsNullOrEmpty(msg.Text))
                        {
                            msg.Source = file;
                            msg.LineNumber = attrLine;
                            yield return msg;
                        }
                    }
                }
            }
        }

        private static Message ExtractFromLocalizeElement(XElement element)
        {
            return new Message
            {
                Text = element.Attribute("Text")?.Value,
                PluralText = element.Attribute("Plural")?.Value,
                Context = element.Attribute("Context")?.Value,
            };
        }

        private static IEnumerable<Message> ExtractFromAttributeValue(string value, IReadOnlySet<string> localizePrefixes)
        {
            int i = 0;
            while (i < value.Length)
            {
                int braceStart = value.IndexOf('{', i);
                if (braceStart < 0) yield break;

                // Skip XAML escaped literal brace {{
                if (braceStart + 1 < value.Length && value[braceStart + 1] == '{')
                {
                    i = braceStart + 2;
                    continue;
                }

                // Find the matching closing brace, tracking nesting depth
                int depth = 1;
                int j = braceStart + 1;
                while (j < value.Length && depth > 0)
                {
                    if (value[j] == '{') depth++;
                    else if (value[j] == '}') depth--;
                    j++;
                }

                if (depth != 0)
                {
                    // Unmatched brace — malformed markup extension, skip ahead
                    i = braceStart + 1;
                    continue;
                }

                // content is everything between { and the matching }
                string content = value.Substring(braceStart + 1, j - braceStart - 2);

                // The type name is the first token (delimited by space, tab, or comma)
                int firstDelim = IndexOfFirst(content, ' ', '\t', ',');
                string typeName = (firstDelim < 0 ? content : content[..firstDelim]).Trim();

                if (IsLocalizeType(typeName, localizePrefixes))
                {
                    string argsContent = firstDelim < 0 ? string.Empty : content[(firstDelim + 1)..];
                    yield return ParseMarkupExtensionArgs(argsContent);
                }

                // Continue scanning from inside this block to pick up nested extensions
                i = braceStart + 1;
            }
        }

        private static bool IsLocalizeNamespace(string namespaceUri)
        {
            // clr-namespace:/using: form — URI contains a known Stride CLR namespace with Localize types
            foreach (var marker in LocalizeClrNamespaceMarkers)
            {
                if (namespaceUri.Contains(marker))
                    return true;
            }
            // Compact form — the well-known Stride XAML URI shared by both WPF and Avalonia assemblies
            return namespaceUri == StrideXamlPresentationNamespace;
        }

        private static bool IsLocalizeLocalName(string localName) =>
            localName == "Localize" || localName == "LocalizeString";

        private static bool IsLocalizeType(string typeName, IReadOnlySet<string> localizePrefixes)
        {
            int colon = typeName.IndexOf(':');
            if (colon < 0)
                return false; // unprefixed — cannot verify namespace, skip defensively
            return IsLocalizeLocalName(typeName[(colon + 1)..])
                && localizePrefixes.Contains(typeName[..colon]);
        }

        private static Message ParseMarkupExtensionArgs(string content)
        {
            var message = new Message();
            bool positionalDone = false;

            foreach (var arg in SplitArgs(content))
            {
                var trimmed = arg.Trim();
                if (trimmed.Length == 0) continue;

                int eq = FindUnnestedEquals(trimmed);
                if (eq >= 0)
                {
                    positionalDone = true;
                    var name = trimmed[..eq].Trim();
                    var val = Unquote(trimmed[(eq + 1)..].Trim());
                    switch (name)
                    {
                        case "Text": message.Text = val; break;
                        case "Plural": message.PluralText = val; break;
                        case "Context": message.Context = val; break;
                        // Count and IsStringFormat are intentionally ignored
                    }
                }
                else if (!positionalDone)
                {
                    // First positional constructor argument maps to Text
                    message.Text = Unquote(trimmed);
                    positionalDone = true;
                }
            }

            return message;
        }

        private static List<string> SplitArgs(string content)
        {
            // Split on top-level commas (not inside nested {} or single-quoted strings)
            var parts = new List<string>();
            int depth = 0;
            bool inQuote = false;
            int start = 0;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '\'' && depth == 0) { inQuote = !inQuote; continue; }
                if (inQuote) continue;
                if (c == '{') depth++;
                else if (c == '}') depth--;
                else if (c == ',' && depth == 0)
                {
                    parts.Add(content[start..i]);
                    start = i + 1;
                }
            }

            if (start < content.Length)
                parts.Add(content[start..]);

            return parts;
        }

        private static int FindUnnestedEquals(string arg)
        {
            int depth = 0;
            for (int i = 0; i < arg.Length; i++)
            {
                char c = arg[i];
                if (c == '{') depth++;
                else if (c == '}') depth--;
                else if (c == '=' && depth == 0) return i;
            }
            return -1;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
                return value[1..^1];
            // Strip XAML literal-brace escape prefix {}Hello → Hello
            if (value.StartsWith("{}"))
                return value[2..];
            return value;
        }

        private static int IndexOfFirst(string s, params char[] chars)
        {
            for (int i = 0; i < s.Length; i++)
            {
                foreach (var c in chars)
                    if (s[i] == c) return i;
            }
            return -1;
        }
    }
}
