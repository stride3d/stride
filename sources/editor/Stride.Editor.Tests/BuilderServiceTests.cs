// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Effect;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Editor.Build;
using Stride.Editor.Tests.Helpers;
using Stride.Shaders.Compiler;

namespace Stride.Editor.Tests;

public sealed class BuilderServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task TheServiceOpensWithItsDatabaseAndEffectCompiler()
    {
        await using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new TestEditorAsset { Value = "wood" }));
        var builder = fixture.Builder;

        Assert.Same(fixture.Session, builder.Session);
        Assert.NotNull(builder.Database);
        Assert.NotNull(builder.EffectCompiler);
        Assert.False(builder.IsDisposed);
        Assert.Equal(0, builder.PendingShaderCompilationCount);
        Assert.Same(builder, fixture.Session.ServiceProvider.Get<GameStudioBuilderService>());
        Assert.Same(builder.Database, fixture.Session.ServiceProvider.Get<GameStudioDatabase>());
    }

    [Fact]
    public async Task TheEffectCompilerReadsTheShaderFilesOfTheSession()
    {
        await using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new TestEditorAsset { Value = "wood" }));
        var builder = fixture.Builder;

        // Nothing is imported: the effects read the shader files where they are, through a provider in front of the shared database
        var provider = Assert.IsType<ShaderSourceFileProvider>(((EffectCompilerBase)builder.EffectCompiler).FileProvider);
        Assert.IsType<DatabaseFileProvider>(provider.Database);
        Assert.Empty(builder.ShaderSources.FileNames);
    }

    [Fact]
    public async Task BuildUnitsRunInPriorityOrder()
    {
        await using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new TestEditorAsset { Value = "wood" }));
        var builder = fixture.Builder;
        var order = new List<string>();
        using var gate = new ManualResetEventSlim(false);

        // The first unit holds the queue until the others are pushed
        var first = new GatedBuildUnit("gate", order, priorityMajor: 0, gate: gate);
        builder.PushBuildUnit(first);
        await ExclusiveDispatcherService.WaitUntilAsync(() => first.Preparing.IsSet, "the first unit to be prepared", Timeout);

        var units = new[]
        {
            new GatedBuildUnit("thumbnail-2", order, DefaultAssetBuilderPriorities.ThumbnailPriority, priorityMinor: 2),
            new GatedBuildUnit("scene", order, DefaultAssetBuilderPriorities.ScenePriority),
            new GatedBuildUnit("thumbnail-1", order, DefaultAssetBuilderPriorities.ThumbnailPriority, priorityMinor: 1),
            new GatedBuildUnit("preview", order, DefaultAssetBuilderPriorities.PreviewPriority),
        };
        foreach (var unit in units)
            builder.PushBuildUnit(unit);
        Assert.Equal(4, builder.QueuedBuildUnitCount);

        gate.Set();
        foreach (var unit in units)
            Assert.Equal(ResultStatus.Successful, await unit.Wait().WaitAsync(Timeout));

        Assert.Equal(["gate", "scene", "preview", "thumbnail-1", "thumbnail-2"], order);
        Assert.All(units, unit => Assert.True(unit.Succeeded));
        Assert.Equal(0, builder.QueuedBuildUnitCount);
    }

    [Fact]
    public async Task ARemovedBuildUnitIsNeverPrepared()
    {
        await using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new TestEditorAsset { Value = "wood" }));
        var builder = fixture.Builder;
        var order = new List<string>();
        using var gate = new ManualResetEventSlim(false);

        var first = new GatedBuildUnit("gate", order, priorityMajor: 0, gate: gate);
        builder.PushBuildUnit(first);
        await ExclusiveDispatcherService.WaitUntilAsync(() => first.Preparing.IsSet, "the first unit to be prepared", Timeout);

        var removed = builder.PushBuildUnit(new GatedBuildUnit("removed", order, priorityMajor: 1));
        var kept = new GatedBuildUnit("kept", order, priorityMajor: 2);
        builder.PushBuildUnit(kept);
        builder.RemoveBuildUnit(removed);
        Assert.Equal(1, builder.QueuedBuildUnitCount);

        gate.Set();
        Assert.Equal(ResultStatus.Successful, await kept.Wait().WaitAsync(Timeout));
        Assert.Equal(["gate", "kept"], order);
    }

    [Fact]
    public async Task AChangeOfAShaderFileABuildReadNamesItsBuildUnits()
    {
        var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new TestEditorAsset { Value = "wood" }));
        var builder = fixture.Builder;
        builder.ShaderChangeDelay = TimeSpan.FromMilliseconds(50);
        var directory = Directory.CreateTempSubdirectory("stride-shader-sources-");
        try
        {
            var shaderFile = Path.Combine(directory.FullName, "Foo.sdsl");
            var unreadFile = Path.Combine(directory.FullName, "Bar.sdsl");
            File.WriteAllText(shaderFile, "shader Foo {};");
            File.WriteAllText(unreadFile, "shader Bar {};");

            // The second unit takes its result from the cache, as every build does after a reopen: it read the file all the same
            var built = new FileReadingBuildUnit(shaderFile);
            builder.PushBuildUnit(built);
            Assert.Equal(ResultStatus.Successful, await built.Wait().WaitAsync(Timeout));
            var cached = new FileReadingBuildUnit(shaderFile);
            builder.PushBuildUnit(cached);
            await cached.Wait().WaitAsync(Timeout);
            Assert.Equal(ResultStatus.NotTriggeredWasSuccessful, cached.CommandStatus);

            var changed = new TaskCompletionSource<ShaderSourcesChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            builder.ShaderSourcesChanged += (_, e) => changed.TrySetResult(e);

            // The watcher can take a moment to start: write until it sees a change. The file no build read changes too
            var deadline = DateTime.UtcNow + Timeout;
            for (var i = 0; !changed.Task.IsCompleted; i++)
            {
                Assert.True(DateTime.UtcNow < deadline, "The change of the shader file was never reported.");
                File.AppendAllText(unreadFile, $"// {i}\n");
                File.AppendAllText(shaderFile, $"// {i}\n");
                await Task.WhenAny(changed.Task, Task.Delay(500));
            }

            var args = await changed.Task;
            Assert.Equal([Path.GetFullPath(shaderFile)], args.Files, StringComparer.OrdinalIgnoreCase);
            var dependents = args.GetDependentAssets(built.Identifier.ContextIdentifier).ToList();
            Assert.Equal(2, dependents.Count);
            Assert.Contains(built.Identifier.AssetIdentifier, dependents);
            Assert.Contains(cached.Identifier.AssetIdentifier, dependents);
        }
        finally
        {
            await fixture.DisposeAsync();
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task DisposingTheServiceStopsIt()
    {
        var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new TestEditorAsset { Value = "wood" }));
        var builder = fixture.Builder;

        await fixture.DisposeAsync();

        Assert.True(builder.IsDisposed);
    }
}
