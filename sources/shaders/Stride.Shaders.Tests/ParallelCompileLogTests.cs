// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// An effect compiler uses one shader loader for all its compilations, and they can run at the same time:
/// each one must only get the errors of the shaders it uses.
/// </summary>
public class ParallelCompileLogTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-parallel-compile-log");

    public ParallelCompileLogTests()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Broken.sdsl"), "shader Broken { float F() { return 1 + ; } };");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Healthy.sdsl"), """
            shader Healthy
            {
                stream float4 ColorTarget : SV_Target0;
                void PSMain() { streams.ColorTarget = float4(1, 0, 0, 1); }
            };
            """);
    }

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ErrorsStayInTheCompileOfTheBrokenShader()
    {
        // The healthy compile starts while the broken one is loading its shader, and waits until it ended
        using var brokenIsLoading = new ManualResetEventSlim();
        using var healthyStarted = new ManualResetEventSlim();
        using var brokenEnded = new ManualResetEventSlim();
        var loader = new GatedLoader(sourceDir.FullName)
        {
            OnLoad = name =>
            {
                if (name == "Broken")
                {
                    brokenIsLoading.Set();
                    Assert.True(healthyStarted.Wait(Timeout));
                }
                else if (name == "Healthy")
                {
                    healthyStarted.Set();
                    Assert.True(brokenEnded.Wait(Timeout));
                }
            },
        };

        var (brokenLog, healthyLog) = (new LoggerResult(), new LoggerResult());
        var broken = Task.Run(() =>
        {
            try
            {
                return Merge(loader, "Broken", brokenLog);
            }
            finally
            {
                brokenEnded.Set();
            }
        });
        var healthy = Task.Run(() =>
        {
            Assert.True(brokenIsLoading.Wait(Timeout));
            return Merge(loader, "Healthy", healthyLog);
        });

        Assert.False(await broken);
        Assert.True(await healthy, healthyLog.ToText());
        Assert.False(healthyLog.HasErrors, healthyLog.ToText());
        Assert.StartsWith(BrokenError, Assert.Single(brokenLog.Messages, m => m.Type == LogMessageType.Error).Text);
    }

    [Fact]
    public async Task ACompileWaitingForTheSameShaderGetsItsErrors()
    {
        // The second compile starts while the first one compiles the shader, so it waits for that result
        using var firstIsLoading = new ManualResetEventSlim();
        var loader = new GatedLoader(sourceDir.FullName)
        {
            OnLoad = _ =>
            {
                firstIsLoading.Set();
                Thread.Sleep(300);
            },
        };

        var (firstLog, secondLog) = (new LoggerResult(), new LoggerResult());
        var first = Task.Run(() => Merge(loader, "Broken", firstLog));
        var second = Task.Run(() =>
        {
            Assert.True(firstIsLoading.Wait(Timeout));
            return Merge(loader, "Broken", secondLog);
        });

        Assert.False(await first);
        Assert.False(await second);
        foreach (var log in new[] { firstLog, secondLog })
        {
            var error = Assert.Single(log.Messages, m => m.Type == LogMessageType.Error);
            Assert.StartsWith(BrokenError, error.Text);
            Assert.Null(((LogMessage)error).Exception);
        }
    }

    private string BrokenError => $"{Path.Combine(sourceDir.FullName, "Broken.sdsl")}(1,";

    private static bool Merge(ShaderLoader loader, string shader, LoggerResult log)
        => new ShaderMixer(loader).MergeSDSL(new ShaderClassSource(shader), new ShaderMixer.Options(true), log, out _, out _, out _, out _);

    private sealed class GatedLoader(params string[] searchPaths) : ShaderLoader(searchPaths)
    {
        public required Action<string> OnLoad { get; init; }

        public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
        {
            OnLoad(name);
            return base.LoadExternalFileContent(name, out filename, out code, out hash);
        }
    }
}
