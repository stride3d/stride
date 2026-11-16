// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The session of the test kit of editor plugins (Stride.Core.Assets.Editor.Testing), with any plugin host: it writes the package,
/// opens it in the dispatcher and cleans everything up.
/// </summary>
public sealed class EditorTestSessionTests
{
    [Fact]
    public async Task TheSessionOpensThePackageInTheDispatcherAndDisposingRemovesIt()
    {
        string directory;
        await using (var fixture = await EditorTestSession.OpenAsync(new EditorTestSessionOptions { Plugins = new FakePluginService(), PackageName = "MyGame" },
            ("Folder/Value", new TestEditorAsset { Value = "A" })))
        {
            directory = fixture.Directory;
            // The test goes on in the dispatcher, where it can change the session
            fixture.Session.Dispatcher.EnsureAccess();
            Assert.Equal("MyGame", fixture.Session.LocalPackages.Single().Name);
            var asset = fixture.Asset<AssetViewModel>("Folder/Value");
            Assert.Equal("/MyGame/Folder/Value", asset.Url);
            Assert.Equal("A", ((TestEditorAsset)asset.Asset).Value);
            Assert.Throws<InvalidOperationException>(() => fixture.Asset<AssetViewModel>("Missing"));
        }
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task SessionOpenedRunsInTheDispatcherWithTheOpenSession()
    {
        var opened = false;
        await using var fixture = await EditorTestSession.OpenAsync(new EditorTestSessionOptions
        {
            Plugins = new FakePluginService(),
            SessionOpened = session =>
            {
                session.Dispatcher.EnsureAccess();
                opened = true;
            },
        });
        Assert.True(opened);
    }
}
