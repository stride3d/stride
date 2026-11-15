// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// An effect compiler uses one shader loader for all its compilations, and they can instantiate MemberName generics at the same time.
/// </summary>
public class ParallelMemberNameCompileTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-parallel-member-name");

    public ParallelMemberNameCompileTests()
    {
        Write("MemberGen", """
            shader MemberGen<MemberName TGroup>
            {
                cbuffer TGroup
                {
                    float GroupValue;
                }

                float MemberValue() { return GroupValue; }
            }
            """);
        Write("RootA", """
            shader RootA : ComputeShaderBase, MemberGen<PerFrameGroup>
            {
                RWBuffer<float> Out;
                override void Compute() { Out[0] = MemberValue(); }
            }
            """);
        Write("RootB", """
            shader RootB : ComputeShaderBase, MemberGen<PerDrawGroup>
            {
                RWBuffer<float> Out;
                override void Compute() { Out[0] = MemberValue(); }
            }
            """);
    }

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EachInstantiationRecordsTheHashOfItsFile()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");

            await Task.WhenAll(Task.Run(() => Merge(loader, "RootA")), Task.Run(() => Merge(loader, "RootB")));

            foreach (var instantiation in new[] { "MemberGen_PerFrameGroup", "MemberGen_PerDrawGroup" })
            {
                Assert.True(loader.Cache.TryLoadFromCache(instantiation, null, [], out var buffer, out _), instantiation);
                Assert.True(loader.IsCachedBufferCurrent(buffer), $"{instantiation} has a source hash that matches no file (attempt {attempt})");
            }
        }
    }

    private static void Merge(ShaderLoader loader, string shader)
    {
        var log = new LoggerResult();
        Assert.True(new ShaderMixer(loader).MergeSDSL(new ShaderClassSource(shader), new ShaderMixer.Options(false), log, out _, out _, out _, out _), log.ToText());
    }

    private void Write(string name, string code) => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);
}
