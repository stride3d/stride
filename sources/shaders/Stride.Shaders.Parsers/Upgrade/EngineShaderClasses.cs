// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text;

namespace Stride.Shaders.Parsing.Upgrade;

/// <summary>
/// The engine shader classes as the upgrade of a project needs them: kind (module or shader), compose type, parents. When a
/// project is upgraded, the engine shaders on disk are those of its old version, so this list of the current engine is
/// embedded instead (<c>EngineShaders.txt</c>, checked against the engine sources by a test).
/// </summary>
public sealed class EngineShaderClasses
{
    private const string ResourceName = "Stride.Shaders.Parsing.Upgrade.EngineShaders.txt";

    internal List<ShaderUpgradeClass> Classes { get; } = [];

    /// <summary>
    /// The list of the engine this assembly belongs to.
    /// </summary>
    public static EngineShaderClasses Current { get; } = Load();

    private static EngineShaderClasses Load()
    {
        using var stream = typeof(EngineShaderClasses).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing resource {ResourceName}");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>
    /// Reads the format of <see cref="Write"/>: one class per line, <c>name, generic parameter count, module|shader, compose|-,
    /// parents</c> separated by tabs; <c>#</c> starts a comment line.
    /// </summary>
    public static EngineShaderClasses Parse(string text)
    {
        var result = new EngineShaderClasses();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;
            var columns = trimmed.Split('\t');
            result.Classes.Add(new ShaderUpgradeClass
            {
                Name = columns[0],
                GenericParameterCount = int.Parse(columns[1]),
                IsModule = columns[2] == "module",
                IsComposeType = columns[3] == "compose",
                Parents = columns.Length > 4 && columns[4].Length > 0 ? [.. columns[4].Split(',')] : [],
            });
        }
        return result;
    }

    /// <summary>
    /// The list for the given engine sources (path → text), already upgraded.
    /// </summary>
    public static string Write(IReadOnlyDictionary<string, string> engineFiles)
    {
        var notes = new List<string>();
        var project = new ShaderUpgradeProject(engineFiles, null, notes);
        if (notes.Count > 0)
            throw new InvalidOperationException($"Engine shaders can't be read:\n{string.Join("\n", notes)}");

        var result = new StringBuilder();
        result.Append("# Engine shader classes for the shader source upgrade (ShaderUpgrader): name, generic parameter count, module|shader, compose|-, parents.\n");
        result.Append("# Generated from the engine .sdsl files by the explicit test ShaderUpgradeTests.UpdateEngineShaderClasses.\n");
        foreach (var shaderClass in project.ProjectClasses.OrderBy(x => x.Name, StringComparer.Ordinal))
            result.Append($"{shaderClass.Name}\t{shaderClass.GenericParameterCount}\t{(shaderClass.IsModule ? "module" : "shader")}\t{(project.IsComposeType(shaderClass.Name) ? "compose" : "-")}\t{string.Join(",", shaderClass.Parents)}\n");
        return result.ToString();
    }
}
