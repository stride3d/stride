// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.FixAssetReferences;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Serialization;
using Stride.Rendering;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// Deleting an asset that other assets reference asks how to fix the references (<see cref="FixAssetReferencesViewModel"/>),
/// as in WPF: replace them with another asset, or clear them. The fixes and the deletion are one undoable step.
/// </summary>
public sealed class FixAssetReferencesTests
{
    private const string SkeletonLocation = "Skeleton";
    private const string OtherSkeletonLocation = "Other Skeleton";
    private const string ModelLocation = "Model";

    [Fact]
    public async Task CancellingTheDialogKeepsTheAsset()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (skeleton, model) = (fixture.Asset(SkeletonLocation), fixture.Asset(ModelLocation));

        Assert.False(await fixture.Session.DeleteItems([skeleton], skipConfirmation: true));

        var fixReferences = Assert.Single(Dialogs(fixture).FixAssetReferencesDialogs);
        Assert.Same(skeleton, fixReferences.CurrentObjectToReplace);
        Assert.Equal("1/1", fixReferences.FixedObjectsCounter);
        var replacement = Assert.Single(fixReferences.CurrentReplacements);
        Assert.Same(model, replacement.Referencer);
        Assert.Same(skeleton, fixture.Session.GetAssetById(skeleton.Id));
        Assert.Equal(skeleton.Id, SkeletonOf(model));
    }

    [Fact]
    public async Task TheReferencesCanBeReplacedWithAnotherAssetInOneUndoableStep()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (skeleton, other, model) = (fixture.Asset(SkeletonLocation), fixture.Asset(OtherSkeletonLocation), fixture.Asset(ModelLocation));
        Dialogs(fixture).FixAssetReferences = fixReferences =>
        {
            Assert.True(fixReferences.UseSingleReplace);
            // Nothing to replace with yet
            Assert.False(fixReferences.FixReferencesCommand.IsEnabled);
            fixReferences.SingleReplacementObject = other;
            Assert.True(fixReferences.FixReferencesCommand.IsEnabled);
            fixReferences.FixReferencesCommand.Execute();
            return Task.FromResult(fixReferences.IsCompleted);
        };

        Assert.True(await fixture.Session.DeleteItems([skeleton], skipConfirmation: true));

        Assert.Null(fixture.Session.GetAssetById(skeleton.Id));
        Assert.Equal(other.Id, SkeletonOf(model));

        fixture.Session.UndoRedoService!.Undo();
        Assert.NotNull(fixture.Session.GetAssetById(skeleton.Id));
        Assert.Equal(skeleton.Id, SkeletonOf(model));
    }

    [Fact]
    public async Task TheReferencesCanBeCleared()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (skeleton, model) = (fixture.Asset(SkeletonLocation), fixture.Asset(ModelLocation));
        Dialogs(fixture).FixAssetReferences = fixReferences =>
        {
            fixReferences.ClearAllReferencesCommand.Execute();
            return Task.FromResult(fixReferences.IsCompleted);
        };

        Assert.True(await fixture.Session.DeleteItems([skeleton], skipConfirmation: true));

        Assert.Null(fixture.Session.GetAssetById(skeleton.Id));
        Assert.Null(((ModelAsset)model.Asset).Skeleton);
    }

    [Fact]
    public async Task EachReferenceCanBeHandledOnItsOwn()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (skeleton, model) = (fixture.Asset(SkeletonLocation), fixture.Asset(ModelLocation));
        Dialogs(fixture).FixAssetReferences = fixReferences =>
        {
            fixReferences.UseSingleReplace = false;
            var replacement = Assert.Single(fixReferences.CurrentReplacements);
            // Each reference needs a replacement or to be cleared
            Assert.False(fixReferences.FixReferencesCommand.IsEnabled);
            replacement.DontFixReference = true;
            Assert.True(fixReferences.FixReferencesCommand.IsEnabled);
            fixReferences.FixReferencesCommand.Execute();
            return Task.FromResult(fixReferences.IsCompleted);
        };

        Assert.True(await fixture.Session.DeleteItems([skeleton], skipConfirmation: true));

        Assert.Null(((ModelAsset)model.Asset).Skeleton);
    }

    [Fact]
    public async Task TheReplacementIsPickedAmongTheOtherAssetsOfTheType()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (skeleton, other) = (fixture.Asset(SkeletonLocation), fixture.Asset(OtherSkeletonLocation));
        var dialogs = Dialogs(fixture);
        dialogs.PickAsset = picker => picker.FilteredAssets.Single();
        dialogs.FixAssetReferences = fixReferences =>
        {
            fixReferences.PickupSingleReplacementObjectCommand.Execute();
            return Task.FromResult(false);
        };

        Assert.False(await fixture.Session.DeleteItems([skeleton], skipConfirmation: true));

        // The deleted asset isn't offered
        var picker = Assert.Single(dialogs.AssetPickers);
        Assert.Same(other, Assert.Single(picker.FilteredAssets));
        Assert.Same(other, Assert.Single(dialogs.FixAssetReferencesDialogs).SingleReplacementObject);
    }

    [Fact]
    public async Task AnotherAssetOfTheSameDeleteIsNotOfferedAsAReplacement()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (skeleton, other) = (fixture.Asset(SkeletonLocation), fixture.Asset(OtherSkeletonLocation));
        var dialogs = Dialogs(fixture);
        dialogs.PickAsset = _ => null;
        dialogs.FixAssetReferences = fixReferences =>
        {
            fixReferences.PickupSingleReplacementObjectCommand.Execute();
            return Task.FromResult(false);
        };

        // Nothing references the other skeleton, but it goes with this delete too
        Assert.False(await fixture.Session.DeleteItems([skeleton, other], skipConfirmation: true));

        var picker = Assert.Single(dialogs.AssetPickers);
        Assert.Empty(picker.FilteredAssets);
    }

    private static async Task<AssetsSessionFixture> OpenAsync()
    {
        var skeleton = new SkeletonAsset();
        var model = new ModelAsset { Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(skeleton.Id, SkeletonLocation) };
        return await AssetsSessionFixture.OpenAsync(false, (SkeletonLocation, skeleton), (OtherSkeletonLocation, new SkeletonAsset()), (ModelLocation, model));
    }

    private static FakeEditorDialogService Dialogs(AssetsSessionFixture fixture) => (FakeEditorDialogService)fixture.ServiceProvider.Get<Core.Assets.Editor.Services.IEditorDialogService>();

    private static AssetId? SkeletonOf(AssetViewModel model)
    {
        var skeleton = ((ModelAsset)model.Asset).Skeleton;
        return skeleton is null ? null : AttachedReferenceManager.GetAttachedReference(skeleton)?.Id;
    }
}
