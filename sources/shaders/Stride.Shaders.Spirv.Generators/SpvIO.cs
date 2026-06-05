// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Spirv.Generators;

public interface ISpvOutput
{
    void AddSource(string hint, string source);
}

public sealed record SpvInputFile(string Path, string Text);

public sealed class DiskSpvOutput(string outputDir) : ISpvOutput
{
    public string OutputDir { get; } = outputDir;

    public void AddSource(string hint, string source)
    {
        Directory.CreateDirectory(OutputDir);
        File.WriteAllText(System.IO.Path.Combine(OutputDir, hint), source);
    }
}
