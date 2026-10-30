// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// What the WPF copies of the session satellites (history, tags, thumbnails, asset log, references, source tracker, the session
/// node presenters, documentation, properties) did and the agnostic copies took over.
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

    [Fact]
    public async Task TheFolderOfTheSelectedAssetIsItsFileFolder()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"));
        var wood = fixture.Session.GetAssetByUrl(EditorSessionFixture.Url("Wood"))!;
        fixture.Assets.SelectAssets([wood]);
        var provider = new AssetInitialDirectoryProvider(fixture.Session);

        // As in WPF, the folder of the asset file, in the asset folder of the package (not next to the package file)
        var assetFolder = new UDirectory(Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, "Assets"));
        Assert.Equal(assetFolder, provider.GetInitialDirectory(new UDirectory("")));
        Assert.Equal(UPath.Combine(assetFolder, new UDirectory("Textures")), provider.GetInitialDirectory(new UDirectory("Textures/")));
    }

    [Fact]
    public async Task TheDocumentationAndThePropertiesCheckTheirArguments()
    {
        using var fixture = await EditorSessionFixture.OpenAsync();
        var documentation = fixture.ServiceProvider.Get<UserDocumentationService>();

        Assert.Throws<ArgumentNullException>(() => documentation.GetMemberDocumentation(null!, typeof(object)));
        Assert.Throws<ArgumentNullException>(() => documentation.GetPropertyKeyDocumentation(null!));
        Assert.Throws<ArgumentNullException>(() => new DocumentationNodeUpdater(null!));
        Assert.Throws<ArgumentNullException>(() => new SessionObjectPropertiesViewModel(null!));
    }
}
