// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// What the WPF copies of the session satellites (history, tags, thumbnails, asset log, references, source tracker) did and the
/// agnostic copies took over.
/// </summary>
public sealed class TestSessionSatellites
{
    [Fact]
    public async Task TheAssetLogRequiresASession()
    {
        using var fixture = await EditorSessionFixture.OpenAsync();

        Assert.Throws<ArgumentNullException>(() => new AssetLogViewModel(fixture.ServiceProvider, null!));
    }

    [Fact]
    public async Task UpdatingFromSourceOpensItsProgressWindowAfterADelay()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"));
        var tracker = fixture.Session.SourceTracker!;

        tracker.UpdateSelectedAssetsFromSourceCommand.Execute();

        // As in WPF, a quick update shows no window
        Assert.Equal("Update assets from source", fixture.Dialogs.ProgressWindows[^1].Title);
        Assert.Equal(500, fixture.Dialogs.ProgressWindowDelays[^1]);
    }
}
