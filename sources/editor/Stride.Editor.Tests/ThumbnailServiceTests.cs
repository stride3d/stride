// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Editor.Tests.Helpers;
using Stride.Editor.Thumbnails;
using Stride.Graphics;

namespace Stride.Editor.Tests;

public sealed class ThumbnailServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// A headless thumbnail service whose compilations are recorded; one compilation can hold the builder's queue.
    /// </summary>
    /// <remarks>
    /// The service refreshes every thumbnail of the session once it learns the game settings (rendering mode, colour space,
    /// graphics profile): the factory drains that initial pass and completes it, so the tests start from an idle service.
    /// </remarks>
    private sealed class Harness : IDisposable
    {
        private readonly ManualResetEventSlim gate = new(false);
        private readonly SemaphoreSlim compiled = new(0);
        private readonly List<AssetItem> compiledAssets = [];
        private volatile bool holdNextCompilation;

        private Harness(ServicesSessionFixture fixture)
        {
            Service = new GameStudioThumbnailService(fixture.Session, fixture.SettingsProvider, fixture.Builder, Compile);
        }

        public GameStudioThumbnailService Service { get; }

        /// <summary>
        /// The names of the compiled assets, in compilation order.
        /// </summary>
        public List<string> Compiled { get; } = [];

        /// <summary>
        /// The assets the initial refresh compiled, in compilation order.
        /// </summary>
        public List<string> InitialRefresh { get; } = [];

        /// <summary>
        /// Set once the held compilation started (the builder's queue is blocked from then on until <see cref="Release"/>).
        /// </summary>
        public ManualResetEventSlim Holding { get; } = new(false);

        /// <summary>
        /// Creates the service and lets its initial refresh of every thumbnail complete.
        /// </summary>
        /// <param name="holdNextCompilation">Whether the first compilation requested by the test blocks the builder until <see cref="Release"/>.</param>
        public static async Task<Harness> CreateAsync(ServicesSessionFixture fixture, bool holdNextCompilation)
        {
            var harness = new Harness(fixture);
            harness.gate.Set();
            await harness.WaitForCompilations(fixture.Session.AllAssets.Count());
            List<AssetItem> initial;
            lock (harness.compiledAssets)
            {
                initial = [.. harness.compiledAssets];
                harness.compiledAssets.Clear();
            }
            harness.InitialRefresh.AddRange(harness.Compiled);
            harness.Compiled.Clear();
            // Complete them so the next requests are new build units rather than continuations of a thumbnail in progress
            foreach (var asset in initial)
                harness.Service.NotifyThumbnailBuilt(new ThumbnailBuiltEventArgs { AssetId = asset.Id, Url = asset.Location, Result = ThumbnailBuildResult.Cancelled });
            if (holdNextCompilation)
            {
                harness.gate.Reset();
                harness.holdNextCompilation = true;
            }
            return harness;
        }

        public void Release() => gate.Set();

        /// <summary>
        /// Waits until <paramref name="count"/> more compilations happened.
        /// </summary>
        public async Task WaitForCompilations(int count)
        {
            for (var i = 0; i < count; i++)
                Assert.True(await compiled.WaitAsync(Timeout), $"Only {Compiled.Count} compilations happened");
        }

        private ListBuildStep Compile(AssetItem asset, GameSettingsAsset gameSettings, bool staticThumbnail)
        {
            if (holdNextCompilation)
            {
                holdNextCompilation = false;
                Holding.Set();
                gate.Wait(Timeout);
            }
            lock (compiledAssets)
            {
                compiledAssets.Add(asset);
                Compiled.Add(asset.Location.GetFileName());
            }
            compiled.Release();
            return new ListBuildStep();
        }

        public void Dispose()
        {
            gate.Set();
            Service.Dispose();
        }
    }

    private static Task<ServicesSessionFixture> OpenAsync(params string[] names)
    {
        return ServicesSessionFixture.OpenAsync(withBuilder: true, names.Select(x => ("Textures/" + x, (Asset)new TestEditorAsset { Value = x })).ToArray());
    }

    [Fact]
    public async Task AssetsAddedFirstBuildBeforeTheOnesAddedLast()
    {
        using var fixture = await OpenAsync("Gate", "A", "B", "C", "D");
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: true);
        var service = harness.Service;

        service.AddThumbnailAssetItems([fixture.Asset("Textures/Gate").AssetItem], QueuePosition.Last);
        Assert.True(harness.Holding.Wait(Timeout));

        service.AddThumbnailAssetItems([fixture.Asset("Textures/A").AssetItem, fixture.Asset("Textures/B").AssetItem], QueuePosition.Last);
        service.AddThumbnailAssetItems([fixture.Asset("Textures/C").AssetItem, fixture.Asset("Textures/D").AssetItem], QueuePosition.First);
        Assert.Equal(4, service.PendingThumbnailCount);

        harness.Release();
        await harness.WaitForCompilations(5);

        // First-position assets come out in reverse order of their request (each one goes to the front), then the last-position ones in order
        Assert.Equal(["Gate", "D", "C", "A", "B"], harness.Compiled);
        Assert.Equal(0, service.PendingThumbnailCount);
    }

    [Fact]
    public async Task IncreasingThePriorityMovesQueuedAssetsToTheFront()
    {
        using var fixture = await OpenAsync("Gate", "A", "B", "C");
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: true);
        var service = harness.Service;

        service.AddThumbnailAssetItems([fixture.Asset("Textures/Gate").AssetItem], QueuePosition.Last);
        Assert.True(harness.Holding.Wait(Timeout));
        service.AddThumbnailAssetItems([fixture.Asset("Textures/A").AssetItem, fixture.Asset("Textures/B").AssetItem, fixture.Asset("Textures/C").AssetItem], QueuePosition.Last);

        // C becomes visible: it is built before A and B
        service.IncreaseThumbnailPriority([fixture.Asset("Textures/C").AssetItem]);
        Assert.Equal(3, service.PendingThumbnailCount);

        harness.Release();
        await harness.WaitForCompilations(4);

        Assert.Equal(["Gate", "C", "A", "B"], harness.Compiled);
    }

    [Fact]
    public async Task AnAssetRequestedAgainWhileBuildingIsBuiltOnceMoreAfterwards()
    {
        using var fixture = await OpenAsync("A");
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: false);
        var service = harness.Service;
        var asset = fixture.Asset("Textures/A");

        service.AddThumbnailAssetItems([asset.AssetItem], QueuePosition.First);
        await harness.WaitForCompilations(1);

        // The build step ran but the thumbnail is not reported built yet: the new request becomes a continuation
        service.AddThumbnailAssetItems([asset.AssetItem], QueuePosition.First);
        Assert.Equal(0, service.PendingThumbnailCount);
        Assert.Single(harness.Compiled);

        service.NotifyThumbnailBuilt(new ThumbnailBuiltEventArgs { AssetId = asset.Id, Url = asset.Url, Result = ThumbnailBuildResult.Succeeded, ThumbnailId = ObjectId.New() });
        await harness.WaitForCompilations(1);

        Assert.Equal(["A", "A"], harness.Compiled);
    }

    [Fact]
    public async Task StopAsyncDropsTheQueuedThumbnailsAndIgnoresLaterRequests()
    {
        using var fixture = await OpenAsync("Gate", "A", "B");
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: true);
        var service = harness.Service;

        service.AddThumbnailAssetItems([fixture.Asset("Textures/Gate").AssetItem], QueuePosition.Last);
        Assert.True(harness.Holding.Wait(Timeout));
        service.AddThumbnailAssetItems([fixture.Asset("Textures/A").AssetItem], QueuePosition.Last);
        Assert.Equal(1, service.PendingThumbnailCount);

        await service.StopAsync().WaitAsync(Timeout);
        Assert.Equal(0, service.PendingThumbnailCount);
        service.AddThumbnailAssetItems([fixture.Asset("Textures/B").AssetItem], QueuePosition.First);
        Assert.Equal(0, service.PendingThumbnailCount);

        harness.Release();
        await harness.WaitForCompilations(1);
        await Task.Delay(200);
        Assert.Equal(["Gate"], harness.Compiled);
    }

    [Fact]
    public async Task ABuiltThumbnailIsReportedWithItsImageAndItsProblemsGoToTheAssetLog()
    {
        using var fixture = await OpenAsync("A");
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: false);
        var service = harness.Service;
        var asset = fixture.Asset("Textures/A");
        var completed = new List<ThumbnailCompletedArgs>();
        service.ThumbnailCompleted += (_, e) => completed.Add(e);

        var log = new LoggerResult();
        log.Warning("The font could not be loaded");
        var png = new MemoryStream(Stride.Core.Assets.Editor.Annotations.DeclaredResources.Read(typeof(ThumbnailServiceTests).Assembly, "StaticThumbnail.png")!);
        service.NotifyThumbnailBuilt(new ThumbnailBuiltEventArgs { AssetId = asset.Id, Url = asset.Url, Result = ThumbnailBuildResult.Succeeded, ThumbnailId = ObjectId.New(), ThumbnailStream = png, BuildLog = log });

        var args = Assert.Single(completed);
        Assert.Equal(asset.Id, args.AssetId);
        var data = Assert.IsType<BitmapThumbnailData>(args.Data);
        await data.PrepareForPresentation(fixture.Session.Dispatcher);
        var image = Assert.IsType<Image>(data.Presenter);
        Assert.True(image.Description.Width > 0);
        // The thumbnail log of the asset shows why the thumbnail is broken
        Assert.Contains(fixture.Session.AssetLog.FilteredMessages.Concat(fixture.Session.AssetLog.Messages), x => x.Text.Contains("font"));
    }

    [Fact]
    public async Task ACancelledBuildReportsNothing()
    {
        using var fixture = await OpenAsync("A");
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: false);
        var service = harness.Service;
        var asset = fixture.Asset("Textures/A");
        var completed = 0;
        service.ThumbnailCompleted += (_, _) => completed++;

        service.NotifyThumbnailBuilt(new ThumbnailBuiltEventArgs { AssetId = asset.Id, Url = asset.Url, Result = ThumbnailBuildResult.Cancelled });

        Assert.Equal(0, completed);
    }

    [Fact]
    public async Task StaticThumbnailsComeFromTheAssemblyAttribute()
    {
        using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Statics/One", new StaticThumbnailAsset { Value = "one" }), ("Textures/A", new TestEditorAsset { Value = "a" }));
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: false);
        var service = harness.Service;

        // The default compiler is static too (a generic image), the attribute replaces it with the declared image
        Assert.True(service.HasStaticThumbnail(typeof(StaticThumbnailAsset)));
        Assert.True(service.HasStaticThumbnail(typeof(TestEditorAsset)));
    }

    [Fact]
    public async Task EveryThumbnailIsRefreshedOnceTheGameSettingsAreKnown()
    {
        using var fixture = await OpenAsync("A", "B", "C");
        using var harness = await Harness.CreateAsync(fixture, holdNextCompilation: false);

        // Every asset of the session is refreshed (their order follows the session's asset collection)
        Assert.Equal(["A", "B", "C"], harness.InitialRefresh.Order());
        Assert.Equal(0, harness.Service.PendingThumbnailCount);
    }
}
