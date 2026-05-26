// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Yaml.Serialization;

namespace Stride.TemplateGenerator;

/// <summary>
/// Subset of <c>!TemplateSample</c> / <c>!Template</c> fields the preprocessor cares about.
/// Loaded from each sample's <c>.sdtpl</c> file; informs template.json emission and (later)
/// the aggregated <c>templates.sdtpls</c> shipped at the package root for GameStudio.
/// </summary>
internal sealed class SdtplMetadata
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? FullDescription { get; set; }
    public string? Group { get; set; }
    public string? DefaultOutputName { get; set; }
    public string? Icon { get; set; }
    public List<string> Screenshots { get; } = new();

    /// <summary>
    /// Per-template opt-in to optional preprocessor-emitted parameters. Values are case-
    /// insensitive; currently recognized: <c>HDR</c>, <c>graphicsProfile</c>, <c>orientation</c>.
    /// Unknown entries are kept (forward-compat for future parameter names) but ignored at
    /// emission time.
    /// </summary>
    public List<string> Parameters { get; } = new();

    public bool HasParameter(string name) =>
        Parameters.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Parses a single <c>.sdtpl</c> file via <see cref="YamlStream"/> (Stride.Core.Yaml's
    /// representation-model API — tree of mapping/scalar/sequence nodes, no typed
    /// deserialization machinery required).
    /// </summary>
    public static SdtplMetadata Parse(string path)
    {
        using var reader = new StreamReader(path);
        var stream = new YamlStream();
        stream.Load(reader);
        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode root)
            return new SdtplMetadata();
        return FromMapping(root);
    }

    /// <summary>
    /// Parses a multi-document <c>templates.sdtpls</c> file (one YAML document per template,
    /// separated by <c>---</c>). Used by GameStudio to load the aggregated metadata shipped at
    /// the package root.
    /// </summary>
    public static IReadOnlyList<SdtplMetadata> ParseAll(string path)
    {
        using var reader = new StreamReader(path);
        var stream = new YamlStream();
        stream.Load(reader);
        var result = new List<SdtplMetadata>(stream.Documents.Count);
        foreach (var doc in stream.Documents)
        {
            if (doc.RootNode is YamlMappingNode root)
                result.Add(FromMapping(root));
        }
        return result;
    }

    private static SdtplMetadata FromMapping(YamlMappingNode root)
    {
        var meta = new SdtplMetadata();
        foreach (var entry in root.Children)
        {
            var key = (entry.Key as YamlScalarNode)?.Value;
            if (key == null)
                continue;
            switch (key)
            {
                case "Id":
                    if (entry.Value is YamlScalarNode idScalar && Guid.TryParse(idScalar.Value, out var g))
                        meta.Id = g;
                    break;
                case "Name":              meta.Name = ScalarOrNull(entry.Value); break;
                case "Description":       meta.Description = ScalarOrNull(entry.Value); break;
                case "FullDescription":   meta.FullDescription = ScalarOrNull(entry.Value); break;
                case "Group":             meta.Group = ScalarOrNull(entry.Value); break;
                case "DefaultOutputName": meta.DefaultOutputName = ScalarOrNull(entry.Value); break;
                case "Icon":              meta.Icon = ScalarOrNull(entry.Value); break;
                case "Screenshots":       AppendScalars(entry.Value, meta.Screenshots); break;
                case "Parameters":        AppendScalars(entry.Value, meta.Parameters); break;
            }
        }
        return meta;
    }

    private static string? ScalarOrNull(YamlNode node) =>
        node is YamlScalarNode s ? s.Value : null;

    private static void AppendScalars(YamlNode node, List<string> sink)
    {
        if (node is not YamlSequenceNode seq)
            return;
        foreach (var item in seq.Children)
        {
            if (item is YamlScalarNode s && s.Value is not null)
                sink.Add(s.Value);
        }
    }
}
