// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The test kit of editor plugins (Stride.Assets.Editor.Testing) as a plugin uses it: a session of Stride's assets with the plugins of
/// the test, stand-in views for the editors it opens, and services that fail when the test didn't give them.
/// </summary>
public sealed class EditorTestKitTests
{
    [Fact]
    public async Task ASessionOpensItsAssetsAndTheEditorsThatHaveAStandInView()
    {
        string directory;
        await using (var fixture = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions
        {
            // The editor plugin knows the editor of a sprite sheet
            Plugins = [new StrideEditorPlugin { EnableThumbnailService = false }],
            StandInEditors = [typeof(SpriteSheetEditorViewModel)],
        },
            ("Sprites/Sheet", new SpriteSheetAsset())))
        {
            directory = fixture.Directory;
            Assert.True(Directory.Exists(directory));
            var sheet = fixture.Asset<SpriteSheetViewModel>("Sprites/Sheet");
            Assert.Equal("/TestGame/Sprites/Sheet", sheet.Url);
            Assert.True(sheet.IsEditable, "The package of the test is a project of the solution, hence editable.");

            var collection = fixture.Session.EditorCollection;
            collection.OpenAssetEditor(sheet);
            Assert.IsType<SpriteSheetEditorViewModel>(Assert.Single(collection.Editors));
            Assert.True(await collection.CloseAllEditorsAsync(false));
        }

        // The session, its background work and its folder are gone
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task TheDialogAndClipboardServicesFailUnlessTheTestGivesThem()
    {
        await using (var fixture = await StrideEditorTestSession.OpenAsync([]))
        {
            var dialogs = fixture.ServiceProvider.Get<IEditorDialogService>();
            await Assert.ThrowsAsync<NotSupportedException>(() => dialogs.MessageBoxAsync("Delete?", MessageBoxButton.YesNo));
            await Assert.ThrowsAsync<NotSupportedException>(() => fixture.ServiceProvider.Get<IClipboardService>().GetTextAsync());
        }

        // A fake of the test replaces the stand-in
        var clipboard = new FakeClipboardService();
        await using var withClipboard = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions { Services = [clipboard] });
        Assert.Same(clipboard, withClipboard.ServiceProvider.Get<IClipboardService>());
    }
}
