// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Tests.Helpers;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// Deleting assets asks for a confirmation in whole sentences, which each language translates.
/// </summary>
public sealed class DeleteConfirmationTests
{
    [Fact]
    public async Task TheConfirmationNamesTheAssetsInAWholeSentence()
    {
        var askBefore = Core.Assets.Editor.Settings.EditorSettings.AskBeforeDeletingAssets.GetValue();
        Core.Assets.Editor.Settings.EditorSettings.AskBeforeDeletingAssets.SetValue(true);
        try
        {
            await ExclusiveDispatcherService.SwitchTo();
            await using var fixture = await AssetsSessionFixture.OpenAsync(false, ("A", new SkeletonAsset()), ("B", new SkeletonAsset()));
            var dialogs = (FakeEditorDialogService)fixture.ServiceProvider.Get<Core.Assets.Editor.Services.IEditorDialogService>();
            // Cancel, the second button
            dialogs.PickButton = buttons => buttons.Last().Result;

            Assert.False(await fixture.Session.DeleteItems([fixture.Asset("A")]));
            Assert.False(await fixture.Session.DeleteItems([fixture.Asset("A"), fixture.Asset("B")]));

            Assert.Equal(["Are you sure you want to delete this asset?", "Are you sure you want to delete these 2 assets?"], dialogs.Messages);
            Assert.Equal(2, fixture.Session.AllAssets.Count());
        }
        finally
        {
            Core.Assets.Editor.Settings.EditorSettings.AskBeforeDeletingAssets.SetValue(askBefore);
        }
    }
}
