// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Serialization;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Rendering;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The content-reference text box of the property grid. Displaying a reference used to write it back at once (Avalonia's
/// default binding trigger), which made the node refresh and the editor loop on every model or animation asset.
/// </summary>
public sealed class ReferencePropertyTests
{
    private const string SkeletonLocation = "Skeleton";
    private const string OtherSkeletonLocation = "OtherSkeleton";
    private const string ModelLocation = "Model";

    [AvaloniaFact]
    public async Task ShowingAReferenceWritesNothingBack()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var model = fixture.Asset(ModelLocation);
            var skeleton = fixture.Asset(SkeletonLocation);
            var textBox = await ShowModelAndFindReferenceBoxAsync(fixture, window, model, skeleton);

            // The box shows the reference, nothing was written back and the history is still empty
            Assert.Equal(skeleton.Url, textBox.Text);
            Assert.Equal(skeleton.Id, ReferencedId(model));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
            Assert.False(fixture.Session.ActionHistory.CanUndo);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task LeavingTheReferenceBoxWithoutEditingChangesNothing()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var model = fixture.Asset(ModelLocation);
            var skeleton = fixture.Asset(SkeletonLocation);
            var textBox = await ShowModelAndFindReferenceBoxAsync(fixture, window, model, skeleton);

            textBox.Focus();
            Assert.True(textBox.IsFocused);
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            MoveFocusAway(window, textBox);
            await WindowTestHelper.PumpAsync();

            Assert.Equal(skeleton.Id, ReferencedId(model));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task EnterCommitsAnEditedReference()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var model = fixture.Asset(ModelLocation);
            var skeleton = fixture.Asset(SkeletonLocation);
            var other = fixture.Asset(OtherSkeletonLocation);
            var textBox = await ShowModelAndFindReferenceBoxAsync(fixture, window, model, skeleton);

            Assert.True(textBox.Focus());
            textBox.Text = other.Url;
            // Typing alone commits nothing, as in the WPF editor
            Assert.Equal(skeleton.Id, ReferencedId(model));

            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(other.Id, ReferencedId(model));
            // The history lists the transaction once the session has told the asset about the change
            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            Assert.True(history.CanUndo);
            Assert.True(history.UndoCommand.IsEnabled);

            // Undo gives the first skeleton back, and Redo becomes available again (the commands refresh once the transactions do)
            history.UndoCommand.Execute();
            Assert.Equal(skeleton.Id, ReferencedId(model));
            await WindowTestHelper.WaitUntilAsync(() => history.RedoCommand.IsEnabled, "the Redo command");
            Assert.True(history.CanRedo);
            Assert.False(history.UndoCommand.IsEnabled);

            history.RedoCommand.Execute();
            Assert.Equal(other.Id, ReferencedId(model));
            await WindowTestHelper.WaitUntilAsync(() => history.UndoCommand.IsEnabled, "the Undo command");
            Assert.False(history.RedoCommand.IsEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task EscapeGivesTheBoundReferenceBack()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var model = fixture.Asset(ModelLocation);
            var skeleton = fixture.Asset(SkeletonLocation);
            var textBox = await ShowModelAndFindReferenceBoxAsync(fixture, window, model, skeleton);

            textBox.Focus();
            textBox.Text = "nonsense";
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            MoveFocusAway(window, textBox);
            await WindowTestHelper.PumpAsync();

            Assert.Equal(skeleton.Url, textBox.Text);
            Assert.Equal(skeleton.Id, ReferencedId(model));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static async Task<ShellSessionFixture> OpenModelWithSkeletonAsync()
    {
        var skeleton = new SkeletonAsset();
        var model = new ModelAsset { Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(skeleton.Id, SkeletonLocation) };
        return await ShellSessionFixture.OpenAsync((SkeletonLocation, skeleton), (OtherSkeletonLocation, new SkeletonAsset()), (ModelLocation, model));
    }

    private static async Task<TextBox> ShowModelAndFindReferenceBoxAsync(ShellSessionFixture fixture, MainWindow window, AssetViewModel model, AssetViewModel skeleton)
    {
        // Selecting the asset generates its properties asynchronously; wait for the grid to show the reference box
        fixture.Session.AssetCollection.SelectAssets([model]);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        TextBox? textBox = null;
        await WindowTestHelper.WaitUntilAsync(() => (textBox = grid.GetVisualDescendants().OfType<TextBox>().SingleOrDefault(x => x.Text == skeleton.Url)) is not null, "the reference box");
        // and settle: a second generation would replace the box
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        Assert.True(textBox!.IsAttachedToVisualTree());
        return textBox;
    }

    private static void MoveFocusAway(MainWindow window, TextBox textBox)
    {
        // The asset view's filter box is always there to take the focus
        var other = window.GetVisualDescendants().OfType<TextBox>().First(x => x != textBox && x.IsEffectivelyVisible);
        Assert.True(other.Focus());
        Assert.False(textBox.IsFocused);
    }

    private static AssetId ReferencedId(AssetViewModel model)
    {
        var skeleton = ((ModelAsset)model.Asset).Skeleton;
        return AttachedReferenceManager.GetAttachedReference(skeleton)?.Id ?? AssetId.Empty;
    }
}
