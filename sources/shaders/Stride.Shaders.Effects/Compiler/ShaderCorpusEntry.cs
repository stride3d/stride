// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stride.Graphics;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// One permutation of the shader corpus: the mixin tree given to the shader mixer, before the platform and profile macros
    /// are added. Stored as JSON so it can be committed and diffed, see <see cref="ReadFile"/> / <see cref="WriteFile"/>.
    /// </summary>
    public sealed class ShaderCorpusEntry
    {
        private string? hash;

        /// <summary>
        /// Creates an entry with no <see cref="Sources"/>.
        /// </summary>
        /// <param name="effectName">The name of the effect.</param>
        /// <param name="profile">The graphics profile the effect was compiled for.</param>
        /// <param name="mixin">The mixin tree, before the platform and profile macros are added.</param>
        public ShaderCorpusEntry(string effectName, GraphicsProfile profile, ShaderMixinSource mixin)
        {
            EffectName = effectName;
            Profile = profile;
            Mixin = mixin;
        }

        /// <summary>
        /// The name of the effect.
        /// </summary>
        public string EffectName { get; }

        /// <summary>
        /// The graphics profile the effect was compiled for.
        /// </summary>
        public GraphicsProfile Profile { get; }

        /// <summary>
        /// The mixin tree given to the shader mixer, before the platform and profile macros are added: one entry compiles
        /// for every platform.
        /// </summary>
        public ShaderMixinSource Mixin { get; }

        /// <summary>
        /// Where this permutation was seen (effect log path, capture tag). Not part of <see cref="Hash"/>.
        /// </summary>
        public SortedSet<string> Sources { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// SHA-256 of the effect name, profile and mixin tree. Two entries with the same hash compile to the same shader.
        /// </summary>
        public string Hash => hash ??= ComputeHash();

        /// <summary>
        /// Stable, readable identifier: the effect name and the first 10 hex digits of <see cref="Hash"/>.
        /// </summary>
        public string Id => $"{EffectName}.{Hash[..10]}";

        private string ComputeHash()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
                WriteEntry(writer, this, includeSources: false);
            return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
        }

        /// <summary>
        /// Reads a corpus file written by <see cref="WriteFile"/>.
        /// </summary>
        /// <param name="path">The path of the JSON file.</param>
        /// <returns>The entries, in the order of the file.</returns>
        public static List<ShaderCorpusEntry> ReadFile(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            return document.RootElement.EnumerateArray().Select(ReadEntry).ToList();
        }

        /// <summary>
        /// Writes the entries sorted by <see cref="Id"/>, so the file only changes where permutations change.
        /// </summary>
        public static void WriteFile(string path, IEnumerable<ShaderCorpusEntry> entries)
        {
            using var stream = File.Create(path);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartArray();
            foreach (var entry in entries.OrderBy(x => x.Id, StringComparer.Ordinal))
                WriteEntry(writer, entry, includeSources: true);
            writer.WriteEndArray();
        }

        /// <summary>
        /// Writes one entry as indented JSON, <see cref="Sources"/> included: the format of a capture file.
        /// </summary>
        /// <param name="entry">The entry to write.</param>
        /// <returns>The JSON text, read back by <see cref="FromJson"/>.</returns>
        public static string ToJson(ShaderCorpusEntry entry)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                WriteEntry(writer, entry, includeSources: true);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Reads one entry written by <see cref="ToJson"/>.
        /// </summary>
        /// <param name="json">The JSON text of the entry.</param>
        /// <returns>The entry, with its <see cref="Sources"/>.</returns>
        /// <exception cref="InvalidDataException">The JSON holds a value the corpus format doesn't use.</exception>
        public static ShaderCorpusEntry FromJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return ReadEntry(document.RootElement);
        }

        private static void WriteEntry(Utf8JsonWriter writer, ShaderCorpusEntry entry, bool includeSources)
        {
            writer.WriteStartObject();
            writer.WriteString("effect", entry.EffectName);
            writer.WriteString("profile", entry.Profile.ToString());
            if (includeSources && entry.Sources.Count > 0)
            {
                writer.WriteStartArray("sources");
                foreach (var source in entry.Sources)
                    writer.WriteStringValue(source);
                writer.WriteEndArray();
            }
            writer.WritePropertyName("mixin");
            WriteSource(writer, entry.Mixin);
            writer.WriteEndObject();
        }

        private static ShaderCorpusEntry ReadEntry(JsonElement element)
        {
            var mixin = (ShaderMixinSource)ReadSource(element.GetProperty("mixin"));
            var effectName = element.GetProperty("effect").GetString()!;
            mixin.Name = effectName;
            var entry = new ShaderCorpusEntry(effectName, Enum.Parse<GraphicsProfile>(element.GetProperty("profile").GetString()!), mixin);
            if (element.TryGetProperty("sources", out var sources))
            {
                foreach (var source in sources.EnumerateArray())
                    entry.Sources.Add(source.GetString()!);
            }
            return entry;
        }

        // Format: a class without generics is a string, other classes are { class, generics?, code? },
        // a mixin is { mixins, compositions?, macros? } and a composition array is a JSON array.
        private static void WriteSource(Utf8JsonWriter writer, ShaderSource source)
        {
            switch (source)
            {
                case ShaderClassSource { GenericArguments: null or { Length: 0 } } classSource:
                    writer.WriteStringValue(classSource.ClassName);
                    break;
                case ShaderClassCode classCode:
                    writer.WriteStartObject();
                    writer.WriteString("class", classCode.ClassName);
                    if (classCode.GenericArguments is { Length: > 0 })
                    {
                        writer.WriteStartArray("generics");
                        foreach (var argument in classCode.GenericArguments)
                            writer.WriteStringValue(argument);
                        writer.WriteEndArray();
                    }
                    if (classCode is ShaderClassString classString)
                        writer.WriteString("code", classString.ShaderSourceCode);
                    writer.WriteEndObject();
                    break;
                case ShaderArraySource arraySource:
                    writer.WriteStartArray();
                    foreach (var value in arraySource.Values)
                        WriteSource(writer, value);
                    writer.WriteEndArray();
                    break;
                case ShaderMixinSource mixinSource:
                    writer.WriteStartObject();
                    writer.WriteStartArray("mixins");
                    foreach (var mixin in mixinSource.Mixins)
                        WriteSource(writer, mixin);
                    writer.WriteEndArray();
                    if (mixinSource.Compositions.Count > 0)
                    {
                        writer.WriteStartObject("compositions");
                        foreach (var composition in mixinSource.Compositions)
                        {
                            writer.WritePropertyName(composition.Key);
                            WriteSource(writer, composition.Value);
                        }
                        writer.WriteEndObject();
                    }
                    if (mixinSource.Macros.Count > 0)
                    {
                        writer.WriteStartArray("macros");
                        foreach (var macro in mixinSource.Macros)
                        {
                            writer.WriteStartArray();
                            writer.WriteStringValue(macro.Name);
                            writer.WriteStringValue(macro.Definition);
                            writer.WriteEndArray();
                        }
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                    break;
                default:
                    throw new NotSupportedException($"Shader source type {source.GetType().Name} can't be stored in the shader corpus");
            }
        }

        private static ShaderSource ReadSource(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return new ShaderClassSource(element.GetString()!);
                case JsonValueKind.Array:
                    var arraySource = new ShaderArraySource();
                    foreach (var value in element.EnumerateArray())
                        arraySource.Add(ReadSource(value));
                    return arraySource;
                case JsonValueKind.Object when element.TryGetProperty("class", out var className):
                    var generics = element.TryGetProperty("generics", out var genericsElement)
                        ? genericsElement.EnumerateArray().Select(x => x.GetString()!).ToArray()
                        : null;
                    if (element.TryGetProperty("code", out var code))
                        return new ShaderClassString(className.GetString()!, code.GetString()!) { GenericArguments = generics };
                    return new ShaderClassSource(className.GetString()!) { GenericArguments = generics };
                case JsonValueKind.Object:
                    var mixinSource = new ShaderMixinSource();
                    foreach (var mixin in element.GetProperty("mixins").EnumerateArray())
                        mixinSource.Mixins.Add((ShaderClassCode)ReadSource(mixin));
                    if (element.TryGetProperty("compositions", out var compositions))
                    {
                        foreach (var composition in compositions.EnumerateObject())
                            mixinSource.Compositions.Add(composition.Name, ReadSource(composition.Value));
                    }
                    if (element.TryGetProperty("macros", out var macros))
                    {
                        foreach (var macro in macros.EnumerateArray())
                            mixinSource.Macros.Add(new ShaderMacro(macro[0].GetString()!, macro[1].GetString()!));
                    }
                    return mixinSource;
                default:
                    throw new InvalidDataException($"Unexpected JSON value {element.ValueKind} in shader corpus");
            }
        }
    }
}
