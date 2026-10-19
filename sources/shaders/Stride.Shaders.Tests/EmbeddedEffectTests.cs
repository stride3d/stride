// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Storage;
using Stride.Core.Yaml.Serialization;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The effects whose bytecode is embedded in Stride.Graphics (SpriteBatch, UIEffect, the fonts...), listed in the
/// <c>EmbeddedEffects.yaml</c> of their folder. Compiled with the shader compiler alone (the asset compiler needs Stride.Graphics).
/// </summary>
public class EmbeddedEffectTests
{
    static readonly string[] Folders = ["Shaders.Bytecodes", "Shaders093.Bytecodes"];

    static string GraphicsDirectory => Path.Combine(RepositoryRoot, "sources", "engine", "Stride.Graphics");

    static string RepositoryRoot
    {
        get
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "sources", "engine", "Stride.Graphics")))
                    return directory.FullName;
            }
            throw new DirectoryNotFoundException($"No repository above {AppContext.BaseDirectory}");
        }
    }

    // Direct3D needs fxc and spirv_to_dxil, which are only on Windows
    static GraphicsPlatform[] Platforms => OperatingSystem.IsWindows()
        ? [GraphicsPlatform.Direct3D11, GraphicsPlatform.Direct3D12, GraphicsPlatform.Vulkan]
        : [GraphicsPlatform.Vulkan];

    const string Generator = "Stride.Shaders.Tests EmbeddedEffectTests.UpdateEmbeddedEffects";

    sealed record EmbeddedEffect(string Name, string Shader, string? FieldName);

    // Profile, Namespace, and Effects: a list of Name, Shader (Name or Name<value, ...>) and optional FieldName
    static (string Profile, string Namespace, List<EmbeddedEffect> Effects) ReadList(string path)
    {
        var yaml = new YamlStream();
        using (var reader = new StreamReader(path))
            yaml.Load(reader);
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        string? Get(YamlMappingNode node, string key) => node.Children.TryGetValue(new YamlScalarNode(key), out var value) ? ((YamlScalarNode)value).Value : null;
        var effects = ((YamlSequenceNode)root.Children[new YamlScalarNode("Effects")]).Children
            .Cast<YamlMappingNode>()
            .Select(x => new EmbeddedEffect(Get(x, "Name")!, Get(x, "Shader")!, Get(x, "FieldName")))
            .ToList();
        return (Get(root, "Profile")!, Get(root, "Namespace")!, effects);
    }

    /// <summary>
    /// The files of the embedded effects, compiled now: path → content.
    /// </summary>
    static Dictionary<string, string> Generate()
    {
        var compiler = new EffectCompiler(null!);
        // Sources given as text with \n line breaks: their hashes, stored in the bytecode, are then the same on every checkout
        var sourceManager = compiler.GetFileShaderLoader().SourceManager;
        foreach (var file in Directory.EnumerateFiles(Path.Combine(GraphicsDirectory, "Shaders"), "*.sdsl"))
        {
            var relative = Path.GetRelativePath(RepositoryRoot, file).Replace('\\', '/');
            sourceManager.AddShaderSource(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file).ReplaceLineEndings("\n"), relative);
        }

        var files = new Dictionary<string, string>();
        foreach (var folder in Folders)
        {
            var list = ReadList(Path.Combine(GraphicsDirectory, folder, "EmbeddedEffects.yaml"));
            foreach (var effect in list.Effects)
            {
                foreach (var platform in Platforms)
                {
                    var parameters = new CompilerParameters();
                    parameters.EffectParameters.Platform = platform;
                    parameters.EffectParameters.Profile = Enum.Parse<GraphicsProfile>(list.Profile);
                    // No debug info, so the same sources always give the same bytes; FXC's default level, as with debug info before
                    parameters.EffectParameters.Debug = false;
                    parameters.EffectParameters.OptimizationLevel = 1;
                    parameters.Set(EffectSourceCodeKeys.Enable, true);
                    parameters.Set(EffectSourceCodeKeys.Namespace, list.Namespace);
                    if (effect.FieldName != null)
                        parameters.Set(EffectSourceCodeKeys.FieldName, effect.FieldName);

                    var source = new ShaderMixinSource { Name = effect.Name };
                    source.Mixins.Add(ParseShader(effect.Shader));
                    var result = compiler.Compile(source, parameters.EffectParameters, parameters, ObjectId.Empty).WaitForResult();
                    Assert.True(result.Bytecode != null, $"{effect.Shader} ({platform}): {string.Join(Environment.NewLine, result.CompilationLog.Messages.Select(m => m.Text))}");

                    var writer = new StringWriter();
                    EffectBytecodeSourceWriter.Write(effect.Name, parameters, result.Bytecode, writer, Generator);
                    files[Path.Combine(GraphicsDirectory, folder, EffectBytecodeSourceWriter.GetFileName(effect.Name, parameters))] = writer.ToString();
                }
            }
        }
        return files;
    }

    // Name or Name<value, value>
    static ShaderClassSource ParseShader(string shader)
    {
        var generics = shader.IndexOf('<');
        return generics < 0
            ? new ShaderClassSource(shader)
            : new ShaderClassSource(shader[..generics], shader[(generics + 1)..^1].Split(',', StringSplitOptions.TrimEntries));
    }

    /// <summary>
    /// Rewrites the embedded effect files of Stride.Graphics. Stride.Graphics refuses to build with a stale bytecode header,
    /// so build this project with <c>-p:StrideSkipPrecompiledBytecodeCheck=true</c> first.
    /// </summary>
    [Fact(Explicit = true)]
    public void UpdateEmbeddedEffects()
    {
        foreach (var (path, content) in Generate())
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    // The committed files are what the compiler gives today
    [Fact]
    public void EmbeddedEffectsAreCurrent()
    {
        var stale = Generate()
            .Where(x => !File.Exists(x.Key) || File.ReadAllText(x.Key).ReplaceLineEndings("\n") != x.Value)
            .Select(x => Path.GetFileName(x.Key))
            .ToList();
        Assert.True(stale.Count == 0,
            $"Embedded effects of Stride.Graphics are stale: {string.Join(", ", stale)}. Regenerate them with the explicit test {nameof(UpdateEmbeddedEffects)} " +
            "(dotnet test sources/shaders/Stride.Shaders.Tests -p:StrideSkipPrecompiledBytecodeCheck=true " +
            "--filter \"FullyQualifiedName~EmbeddedEffectTests.UpdateEmbeddedEffects\" -- xUnit.Explicit=on)");
    }
}
