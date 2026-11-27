// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Stride.AssetCompiler.targets: the asset build runs when an input file differs from the last successful build, or an output is missing.
    /// </summary>
    public sealed class TestAssetCompilerUpToDateTargets : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-asset-compiler-uptodate-{Guid.NewGuid():N}");
        private readonly string project;
        private readonly string model;
        private readonly string engine;
        private readonly string bundle;
        private readonly string runs;
        private readonly string failure;

        public TestAssetCompilerUpToDateTargets()
        {
            Stride.Core.Assets.PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            var game = Path.Combine(directory, "Game");
            project = Path.Combine(game, "Game.csproj");
            model = Path.Combine(game, "Resources", "Model.fbx");
            bundle = Path.Combine(game, "bin", "data", "default.bundle");
            runs = Path.Combine(directory, "runs.txt");
            failure = Path.Combine(directory, "fail");
            Directory.CreateDirectory(Path.GetDirectoryName(model)!);
            Directory.CreateDirectory(Path.GetDirectoryName(bundle)!);
            File.WriteAllText(model, "model");
            File.SetLastWriteTimeUtc(model, DateTime.UtcNow.AddHours(-1));

            // An engine assembly next to the compiler, as in the compiler's own output folder of a source checkout
            engine = Path.Combine(directory, "Engine.dll");
            File.WriteAllText(engine, "engine");
            File.SetLastWriteTimeUtc(engine, DateTime.UtcNow.AddHours(-1));

            // A fake asset compiler: like the real one, it writes the bundle and, after a successful build, the lists of its inputs and outputs
            var fileBase = Path.Combine(game, "obj", "stride", "assetcompiler-uptodatecheck");
            var inputs = Path.Combine(directory, "inputs.txt");
            var outputs = Path.Combine(directory, "outputs.txt");
            File.WriteAllLines(inputs, [Path.Combine(game, "Resources", "**", "*.*")]);
            File.WriteAllLines(outputs, [bundle]);
            string compiler, host;
            if (OperatingSystem.IsWindows())
            {
                compiler = Path.Combine(directory, "compiler.cmd");
                host = $"call \"{compiler}\"";
                File.WriteAllLines(compiler, [
                    "@echo off",
                    $"if exist \"{failure}\" exit /b 1",
                    $"echo run>>\"{runs}\"",
                    $"copy /y \"{model}\" \"{bundle}\" >nul",
                    $"copy /y \"{inputs}\" \"{fileBase}.inputs\" >nul",
                    $"copy /y \"{outputs}\" \"{fileBase}.outputs\" >nul",
                ]);
            }
            else
            {
                compiler = Path.Combine(directory, "compiler.sh");
                host = $"sh \"{compiler}\"";
                File.WriteAllLines(compiler, [
                    $"[ -e \"{failure}\" ] && exit 1",
                    $"echo run >> \"{runs}\"",
                    $"cp \"{model}\" \"{bundle}\"",
                    $"cp \"{inputs}\" \"{fileBase}.inputs\"",
                    $"cp \"{outputs}\" \"{fileBase}.outputs\"",
                ]);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(fileBase)!);

            File.WriteAllText(project, $"""
                <Project>
                  <PropertyGroup>
                    <ProjectDir>{game}{Path.DirectorySeparatorChar}</ProjectDir>
                    <IntermediateOutputPath>obj{Path.DirectorySeparatorChar}</IntermediateOutputPath>
                    <TargetDir>{Path.GetDirectoryName(Path.GetDirectoryName(bundle))}{Path.DirectorySeparatorChar}</TargetDir>
                    <OutputType>Exe</OutputType>
                    <StrideAssetCompilerHost>{host}</StrideAssetCompilerHost>
                    <StrideCompileAssetCommand>{compiler}</StrideCompileAssetCommand>
                    <StrideAssetBuildManifestPath>{compiler}</StrideAssetBuildManifestPath>
                    <STRIDE_CRASH_MODE>off</STRIDE_CRASH_MODE>
                  </PropertyGroup>
                  <Import Project="{TestAssetCompilerPathTargets.TargetsPath()}" />
                </Project>
                """);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void BuildWithNoChangeSkipsTheCompiler()
        {
            // The first build is the one that lists the inputs: the second one has nothing to do
            Build();
            Assert.Equal(1, Runs());

            Build();
            Build();
            Assert.Equal(1, Runs());
        }

        [Fact]
        public void OlderFileRunsTheCompiler()
        {
            Build();

            // A copy exported earlier replaces the file
            File.WriteAllText(model, "older model");
            File.SetLastWriteTimeUtc(model, File.GetLastWriteTimeUtc(model).AddDays(-1));
            Build();
            Assert.Equal(2, Runs());

            Build();
            Assert.Equal(2, Runs());
        }

        [Fact]
        public void NewSizeAtTheSameDateRunsTheCompiler()
        {
            Build();

            var date = File.GetLastWriteTimeUtc(model);
            File.WriteAllText(model, "a longer model");
            File.SetLastWriteTimeUtc(model, date);
            Build();
            Assert.Equal(2, Runs());
        }

        [Fact]
        public void NewCompilerFileRunsTheCompiler()
        {
            Build();

            // The engine is rebuilt, the game is not
            File.WriteAllText(engine, "rebuilt engine");
            Build();
            Assert.Equal(2, Runs());

            Build();
            Assert.Equal(2, Runs());
        }

        [Fact]
        public void FileDatedInTheFutureRunsTheCompilerOnce()
        {
            File.SetLastWriteTimeUtc(model, DateTime.UtcNow.AddDays(1));

            Build();
            Build();
            Assert.Equal(1, Runs());
        }

        [Fact]
        public void MissingOutputRunsTheCompiler()
        {
            Build();

            File.Delete(bundle);
            Build();
            Assert.Equal(2, Runs());

            Build();
            Assert.Equal(2, Runs());
        }

        [Fact]
        public void FailedBuildRunsTheCompilerAgain()
        {
            Build();

            File.WriteAllText(model, "new model");
            File.WriteAllText(failure, "");
            Assert.False(new ProjectInstance(project, null, null, new ProjectCollection()).Build("StrideCompileAsset", null));
            File.Delete(failure);

            Build();
            Assert.Equal(2, Runs());
        }

        private void Build()
        {
            Assert.True(new ProjectInstance(project, null, null, new ProjectCollection()).Build("StrideCompileAsset", null));
        }

        private int Runs() => File.Exists(runs) ? File.ReadAllLines(runs).Length : 0;
    }
}
