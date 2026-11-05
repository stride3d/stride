// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels.Logs;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Editor.Tests.Helpers;

namespace Stride.Editor.Tests;

/// <summary>
/// The asset log of a session, whose logs change through the dispatcher while the builder and the thumbnails run.
/// </summary>
public sealed class AssetLogTests
{
    [Fact]
    public async Task ARefreshStartedOutsideTheDispatcherReadsTheLogsOnIt()
    {
        var packageDirectory = ServicesSessionFixture.NewPackageDirectory();
        var source = ServicesSessionFixture.WriteResource(packageDirectory, "wood.txt", "wood");
        using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: false, ("Textures/Wood", new RawAsset { Source = new UFile(source) }));
        var log = fixture.Session.AssetLog;
        var wood = fixture.Asset("Textures/Wood");

        // A change made on the thread pool starts the delayed refresh of the shown messages there
        await Task.Run(() =>
        {
            var result = new LoggerResult();
            result.Warning("A message logged outside the dispatcher");
            log.AddLogger(LogKey.Get("Outside"), result);
        });
        // Meanwhile the logs of an asset are replaced through the dispatcher, as each build of the asset replaces its log, until
        // after the refresh ran: read on the thread pool, the logs changed under it and the exception ended the process
        var end = DateTime.UtcNow.AddMilliseconds(1500);
        var last = await Task.Run(() =>
        {
            var i = 0;
            for (; DateTime.UtcNow < end; ++i)
            {
                var result = new LoggerResult();
                result.Warning($"Build {i}");
                log.SetAssetLog(wood.Id, $"{AssetLogViewModel.BuildLogName} {i % 50}", result);
            }
            return i - 1;
        });

        // The shown messages end up with the last ones, read on the dispatcher, where they change
        var dispatcher = fixture.ServiceProvider.Get<IDispatcherService>();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!dispatcher.Invoke(() => log.FilteredMessages.Any(x => x.Text == $"Build {last}") && log.FilteredMessages.Any(x => x.Text == "A message logged outside the dispatcher")))
        {
            Assert.True(DateTime.UtcNow < deadline, "The asset log did not show the last messages.");
            await Task.Delay(50);
        }
        // A refresh that the last change started may still be waiting for its delay: it must not outlive the test
        await Task.Delay(1000);
    }
}
