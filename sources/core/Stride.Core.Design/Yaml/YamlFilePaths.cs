// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml;

/// <summary>
/// Visits the file paths of an asset in its YAML text, without loading the asset: the values tagged <c>!file</c>, and the
/// keys of <c>~SourceHashes</c>, which hold a path after their <c>~</c>.
/// </summary>
public static class YamlFilePaths
{
    /// <summary>The tag every save writes on a file path.</summary>
    public const string FileTag = "!file";

    /// <summary>The member of an asset that maps its source files to their hashes.</summary>
    public const string SourceHashesKey = "~SourceHashes";

    /// <summary>
    /// Replaces the file paths of a text. Only the paths change: each one keeps its tag and its quotes, or gets quotes if
    /// the new path needs them, and every other character stays as it is.
    /// </summary>
    /// <param name="text">The text of the asset, with or without a byte order mark.</param>
    /// <param name="rewrite">
    /// Takes a path, and whether it is a key of <c>~SourceHashes</c>. Returns the new path, or null to keep it. Paths come
    /// in the order of the text, and a call with null changes nothing, which also makes this a way to read them.
    /// </param>
    /// <returns>The new text, or null when nothing changed, as for a text that isn't YAML.</returns>
    /// <exception cref="InvalidOperationException">A path to change is written on several lines or as a block, which no save does.</exception>
    public static string? Rewrite(string text, Func<string, bool, string?> rewrite)
    {
        // The parser reads a byte order mark as content, as a file reader would have removed it.
        var start = text.Length > 0 && text[0] == 0xFEFF ? 1 : 0;
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(text[start..]));
        }
        catch (YamlException)
        {
            return null;
        }

        var paths = new List<(YamlScalarNode Node, string Path, bool IsSourceHash)>();
        foreach (var document in stream.Documents)
            CollectPaths(document.RootNode, paths);

        StringBuilder? rewritten = null;
        var position = 0;
        foreach (var (node, path, isSourceHash) in paths.OrderBy(found => found.Node.Start.Index))
        {
            // An alias visits the node it names a second time.
            if (start + node.Start.Index < position || rewrite(path, isSourceHash) is not { } newPath || newPath == path)
                continue;

            var end = start + node.End.Index;
            var scalarStart = FindScalarStart(text, node, start + node.Start.Index, end);

            rewritten ??= new StringBuilder(text.Length);
            rewritten.Append(text, position, scalarStart - position).Append(Emit(node.Value[..^path.Length] + newPath, node.Style));
            position = end;
        }

        return rewritten?.Append(text, position, text.Length - position).ToString();
    }

    // A node starts at its anchor or tag, and ends with its last quote, if any.
    private static int FindScalarStart(string text, YamlScalarNode node, int start, int end)
        => node.Style switch
        {
            ScalarStyle.SingleQuoted => text.IndexOf('\'', start, end - start),
            ScalarStyle.DoubleQuoted => text.IndexOf('"', start, end - start),
            ScalarStyle.Plain when end - node.Value.Length >= start && string.CompareOrdinal(text, end - node.Value.Length, node.Value, 0, node.Value.Length) == 0 => end - node.Value.Length,
            _ => throw new InvalidOperationException($"The path '{node.Value}' is written in a form that can't be rewritten."),
        };

    // The text of a scalar, in the given style if the value allows it. The emitter decides that. In a flow sequence, as the
    // strictest place a scalar can be, the result is valid wherever the scalar is.
    private static string Emit(string value, ScalarStyle style)
    {
        var writer = new StringWriter();
        var emitter = new Emitter(writer);
        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());
        emitter.Emit(new SequenceStart(null, null, true, DataStyle.Compact));
        emitter.Emit(new Scalar(null, null, value, style, true, true));
        emitter.Emit(new SequenceEnd());
        emitter.Emit(new DocumentEnd(true));
        emitter.Emit(new StreamEnd());

        var emitted = writer.ToString();
        var open = emitted.IndexOf('[') + 1;
        return emitted[open..emitted.LastIndexOf(']')];
    }

    private static void CollectPaths(YamlNode node, List<(YamlScalarNode, string, bool)> paths)
    {
        switch (node)
        {
            case YamlScalarNode scalar when scalar.Tag == FileTag:
                paths.Add((scalar, scalar.Value, false));
                break;
            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                    CollectPaths(child, paths);
                break;
            case YamlMappingNode mapping:
                foreach (var (key, value) in mapping.Children)
                {
                    if (key is YamlScalarNode { Value: SourceHashesKey } && value is YamlMappingNode hashes)
                    {
                        foreach (var hash in hashes.Children.Select(child => child.Key).OfType<YamlScalarNode>())
                            paths.Add((hash, hash.Value[(hash.Value.IndexOf('~') + 1)..], true));
                    }
                    else
                    {
                        CollectPaths(value, paths);
                    }
                }
                break;
        }
    }
}
