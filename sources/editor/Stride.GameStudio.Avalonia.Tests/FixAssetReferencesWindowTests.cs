// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.FixAssetReferences;
using Stride.Core.Serialization;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Rendering;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The window of the references to fix before an asset is deleted (<see cref="FixAssetReferencesWindow"/>).
/// </summary>
public sealed class FixAssetReferencesWindowTests
{
    private const string SkeletonLocation = "Skeleton";
    private const string OtherSkeletonLocation = "Other Skeleton";
    private const string ModelLocation = "Model";

    [AvaloniaFact]
    public async Task TheWindowShowsTheReferencesAndClosesWhenTheyAreFixed()
    {
        using var fixture = await OpenModelWithSkeletonsAsync();
        var owner = new MainWindow { DataContext = fixture.Main };
        owner.Show();
        FixAssetReferencesWindow? window = null;
        try
        {
            var (skeleton, other, model) = (fixture.Asset(SkeletonLocation), fixture.Asset(OtherSkeletonLocation), fixture.Asset(ModelLocation));
            var fixReferences = Create(fixture, skeleton);
            window = new FixAssetReferencesWindow { DataContext = fixReferences };
            var result = window.ShowDialog<bool>(owner);
            await WindowTestHelper.PumpAsync();

            Assert.Equal("Fix references (1/1)", window.Title);
            Assert.Contains(skeleton.Url, Named<TextBlock>(window, "DescriptionText").Text);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == model.Url);
            var done = Named<Button>(window, "DoneButton");
            Assert.False(done.IsEffectivelyEnabled);

            fixReferences.SingleReplacementObject = other;
            await WindowTestHelper.PumpAsync();
            Assert.Equal(other.Url, Named<TextBox>(window, "SingleReplacementTextBox").Text);
            Assert.True(done.IsEffectivelyEnabled);

            await window.ClickAsync(done);
            Assert.True(await result.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.True(fixReferences.IsCompleted);
        }
        finally
        {
            // A dialog left open by a failed assertion would keep its owner open
            window?.Close();
            await owner.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task EachReferenceCanBeClearedOnItsOwn()
    {
        using var fixture = await OpenModelWithSkeletonsAsync();
        var owner = new MainWindow { DataContext = fixture.Main };
        owner.Show();
        FixAssetReferencesWindow? window = null;
        try
        {
            var fixReferences = Create(fixture, fixture.Asset(SkeletonLocation));
            window = new FixAssetReferencesWindow { DataContext = fixReferences };
            var result = window.ShowDialog<bool>(owner);
            await WindowTestHelper.PumpAsync();

            Named<RadioButton>(window, "IndividualReplaceRadioButton").IsChecked = true;
            await WindowTestHelper.PumpAsync();
            Assert.False(fixReferences.UseSingleReplace);
            Assert.False(Named<Button>(window, "DoneButton").IsEffectivelyEnabled);

            Named<CheckBox>(window, "ClearCheckBox").IsChecked = true;
            await WindowTestHelper.PumpAsync();
            Assert.True(Assert.Single(fixReferences.CurrentReplacements).DontFixReference);
            Assert.True(Named<Button>(window, "DoneButton").IsEffectivelyEnabled);

            await window.ClickAsync(Named<Button>(window, "DoneButton"));
            Assert.True(await result.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            // A dialog left open by a failed assertion would keep its owner open
            window?.Close();
            await owner.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task CancelClosesTheWindowWithoutFixing()
    {
        using var fixture = await OpenModelWithSkeletonsAsync();
        var owner = new MainWindow { DataContext = fixture.Main };
        owner.Show();
        FixAssetReferencesWindow? window = null;
        try
        {
            var fixReferences = Create(fixture, fixture.Asset(SkeletonLocation));
            window = new FixAssetReferencesWindow { DataContext = fixReferences };
            var result = window.ShowDialog<bool>(owner);
            await WindowTestHelper.PumpAsync();

            await window.ClickAsync(Named<Button>(window, "CancelButton"));

            Assert.False(await result.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.False(fixReferences.IsCompleted);
        }
        finally
        {
            // A dialog left open by a failed assertion would keep its owner open
            window?.Close();
            await owner.CloseAsync();
        }
    }

    private static FixAssetReferencesViewModel Create(ShellSessionFixture fixture, Core.Assets.Editor.ViewModel.AssetViewModel deleted)
    {
        var fixReferences = new FixAssetReferencesViewModel(fixture.Session.ServiceProvider, fixture.Session, [deleted], fixture.Session.DependencyManager);
        fixReferences.Initialize([deleted]);
        return fixReferences;
    }

    private static T Named<T>(Window window, string name) where T : Control
    {
        return window.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
    }

    private static async Task<ShellSessionFixture> OpenModelWithSkeletonsAsync()
    {
        var skeleton = new SkeletonAsset();
        var model = new ModelAsset { Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(skeleton.Id, SkeletonLocation) };
        return await ShellSessionFixture.OpenAsync((SkeletonLocation, skeleton), (OtherSkeletonLocation, new SkeletonAsset()), (ModelLocation, model));
    }
}
