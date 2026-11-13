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
        Assert.Same(builder, fixture.ServiceProvider.Get<GameStudioBuilderService>());
        Assert.Same(builder.Database, fixture.ServiceProvider.Get<GameStudioDatabase>());
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
    public async Task DisposingTheServiceStopsIt()
    {
        var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new TestEditorAsset { Value = "wood" }));
        var builder = fixture.Builder;

        await fixture.DisposeAsync();

        Assert.True(builder.IsDisposed);
    }
}
