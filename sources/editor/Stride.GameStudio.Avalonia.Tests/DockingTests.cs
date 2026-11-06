// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Dock.Model.Core;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Assets.Sprite;
using Stride.Core;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Docking;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The panels and editors of the main window, docked as WPF's: the View menu shows and hides the panels in their place, a
/// closed panel hides, the editors are documents with a close button that asks once to save, the layouts with and without
/// editors switch and are kept per session, a reset puts the defaults back, and a floating window keeps its views and the
/// shortcuts of the main window.
/// </summary>
public sealed class DockingTests
{
    private const string Script = "public sealed class Player : SyncScript\n{\n    public override void Update() { }\n}\n";

    [AvaloniaFact]
    public async Task TheViewMenuListsThePanelsInTheSectionsOfWpfThenResetLayout()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var menu = window.View.FindControl<MenuItem>("ViewMenu")!;
            Assert.True(menu.IsEffectivelyEnabled);
            var headers = menu.Items.Select(x => x is MenuItem item ? item.Header as string : "-").ToList();
            Assert.Equal(
            [
                "Solution explorer", "Asset view", "References", "-",
                "Asset preview", "-",
                "Property grid", "Edit history", "-",
                "Asset errors", "Output", "-",
                // The panel of the test plugin (PanelPluginTests)
                "New Notebook", "-",
                "Reset layout",
            ], headers);
            // Every panel is visible in the default layout, and its entry is checked
            Assert.All(menu.Items.OfType<MenuItem>().Where(x => x.ToggleType == MenuItemToggleType.CheckBox), x => Assert.True(x.IsChecked));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheViewMenuHidesAPanelAndShowsItAgainInItsPlace()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var panels = fixture.Main.Panels;
            var grid = panels.FindDockable(BuiltInPanels.PropertyGrid)!;
            var group = Assert.IsAssignableFrom<IDock>(grid.Owner);
            Assert.Single(window.GetVisualDescendants().OfType<PropertyGridView>());

            var entry = fixture.Main.ViewMenu.Items.Single(x => x.PanelId == BuiltInPanels.PropertyGrid);
            entry.IsChecked = false;
            await WindowTestHelper.PumpAsync();
            Assert.False(panels.IsVisible(BuiltInPanels.PropertyGrid));
            Assert.Empty(window.GetVisualDescendants().OfType<PropertyGridView>());
            Assert.False(grid.ViewModel.IsShown);

            entry.IsChecked = true;
            await WindowTestHelper.WaitUntilAsync(() => grid.ViewModel.IsShown, "the property grid shown again");
            Assert.True(panels.IsVisible(BuiltInPanels.PropertyGrid));
            Assert.Same(group, grid.Owner);
            Assert.Single(window.GetVisualDescendants().OfType<PropertyGridView>());
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheCloseButtonOfAPanelHidesIt()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var panels = fixture.Main.Panels;
            var references = panels.FindDockable(BuiltInPanels.References)!;
            var entry = fixture.Main.ViewMenu.Items.Single(x => x.PanelId == BuiltInPanels.References);

            panels.Factory.CloseDockable(references);
            await WindowTestHelper.WaitUntilAsync(() => !panels.IsVisible(BuiltInPanels.References), "the panel hidden");

            // Hidden, as in WPF: the same panel comes back from the View menu
            Assert.False(entry.IsChecked);
            Assert.Same(references, panels.FindDockable(BuiltInPanels.References));
            entry.IsChecked = true;
            await WindowTestHelper.PumpAsync();
            Assert.True(panels.IsVisible(BuiltInPanels.References));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheFirstEditorBringsTheLayoutWithEditorsAndTheLastOneTakesItAway()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var panels = fixture.Main.Panels;
            Assert.Equal(PanelLayoutKind.Normal, panels.LayoutKind);
            // Without editors, the asset view takes the centre
            Assert.Equal(DefaultPanelLayouts.CenterGroup, Assert.IsAssignableFrom<IDock>(panels.FindDockable(BuiltInPanels.AssetView)!.Owner).Id);

            var editor = await OpenEditorAsync<SpriteSheetEditorViewModel>(fixture, fixture.Asset("Sheet"));
            Assert.Equal(PanelLayoutKind.Editors, panels.LayoutKind);
            var document = panels.FindDocument(editor)!;
            Assert.Equal("Sheet", document.Title);
            Assert.Single(window.GetVisualDescendants().OfType<SpriteSheetEditorView>());

            // The close button of its tab closes the editor; the asset view gets the centre back
            panels.Factory.CloseDockable(document);
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.EditorCollection.Editors.Count == 0, "the close of the editor");
            await WindowTestHelper.PumpAsync();
            Assert.Equal(PanelLayoutKind.Normal, panels.LayoutKind);
            Assert.Null(panels.FindDocument(editor));
            Assert.Empty(window.GetVisualDescendants().OfType<SpriteSheetEditorView>());
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AnEditorWhoseTabIsNotSelectedLeavesTheWindow()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()), ("Other", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var first = await OpenEditorAsync<SpriteSheetEditorViewModel>(fixture, fixture.Asset("Sheet"));
            var second = await OpenEditorAsync<SpriteSheetEditorViewModel>(fixture, fixture.Asset("Other"));

            // Only the view of the selected editor is in the window: an editor's game stops while its view is detached
            var view = Assert.Single(window.GetVisualDescendants().OfType<SpriteSheetEditorView>());
            Assert.Same(second, view.DataContext);

            // Selecting the other tab makes its editor the active one, and back
            fixture.Main.Panels.Factory.SetActiveDockable(fixture.Main.Panels.FindDocument(first)!);
            await WindowTestHelper.PumpAsync();
            Assert.Same(first, fixture.Session.EditorCollection.ActiveEditor);
            Assert.Same(first, Assert.Single(window.GetVisualDescendants().OfType<SpriteSheetEditorView>()).DataContext);
            fixture.Session.EditorCollection.ActiveEditor = second;
            await WindowTestHelper.PumpAsync();
            Assert.Same(second, Assert.Single(window.GetVisualDescendants().OfType<SpriteSheetEditorView>()).DataContext);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ClosingAnEditorTabAsksOnceToSaveItsChanges()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Player", new ScriptSourceFileAsset { Text = Script }));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var script = fixture.Session.AllAssets.OfType<ScriptSourceFileViewModel>().Single();
            var editor = await OpenEditorAsync<ScriptSourceFileEditorViewModel>(fixture, script);
            var code = window.GetVisualDescendants().OfType<ScriptSourceFileEditorView>().Single().GetVisualDescendants().OfType<Stride.Core.Presentation.Avalonia.Controls.CodeTextEditor>().Single();
            code.TextArea.Focus();
            code.CaretOffset = 0;
            window.KeyTextInput("// ");
            await WindowTestHelper.WaitUntilAsync(() => editor.IsModified, "the edit");
            var panels = fixture.Main.Panels;
            var document = panels.FindDocument(editor)!;

            // Two clicks on the close button: one question; Cancel keeps the editor
            dialogs.PickButton = buttons => buttons.Single(x => x.IsCancel).Result;
            panels.Factory.CloseDockable(document);
            panels.Factory.CloseDockable(document);
            await WindowTestHelper.PumpAsync();
            await WindowTestHelper.PumpAsync();
            Assert.Single(dialogs.Messages);
            Assert.Same(editor, Assert.Single(fixture.Session.EditorCollection.Editors));
            Assert.Same(document, panels.FindDocument(editor));

            // Don't save: the editor closes
            dialogs.PickButton = buttons => buttons.Single(x => !x.IsDefault && !x.IsCancel).Result;
            panels.Factory.CloseDockable(document);
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.EditorCollection.Editors.Count == 0, "the close of the editor");
            Assert.Equal(2, dialogs.Messages.Count);
            Assert.Equal(Script, File.ReadAllText(editor.Asset.AssetItem.FullPath.ToOSPath()));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheLayoutOfASessionIsSavedAndComesBack()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        DockPanelService? reopened = null;
        try
        {
            var panels = fixture.Main.Panels;
            panels.Hide(BuiltInPanels.References);
            panels.Show(BuiltInPanels.BuildLog);
            await WindowTestHelper.PumpAsync();
            panels.SaveLayout();
            var expected = panels.Snapshot();

            // In the settings file, under the session
            var store = GameStudioInternalSettings.CreateSessionPanelLayouts();
            var saved = store.Load(fixture.PackagePath, PanelLayoutKind.Normal);
            Assert.NotNull(saved);
            Assert.Contains(BuiltInPanels.References, saved.Hidden);
            Assert.Equal(new PanelRef(BuiltInPanels.BuildLog), saved.FindGroupOf(new PanelRef(BuiltInPanels.BuildLog))!.Active);

            // The next time the session opens, the panels are where they were
            reopened = new DockPanelService(fixture.ServiceProvider, panels.Registry, store);
            reopened.OpenSession(fixture.Session, fixture.PackagePath);
            Assert.Equal(expected.EnumeratePanels(), reopened.Snapshot().EnumeratePanels());
            Assert.Equal(expected.Hidden, reopened.Snapshot().Hidden);
            Assert.False(reopened.IsVisible(BuiltInPanels.References));
        }
        finally
        {
            reopened?.Dispose();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ResetLayoutShowsEveryPanelInItsDefaultPlace()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var panels = fixture.Main.Panels;
            panels.Hide(BuiltInPanels.PropertyGrid);
            panels.Factory.FloatDockable(panels.FindDockable(BuiltInPanels.ActionHistory)!);
            await WindowTestHelper.PumpAsync();
            Assert.Single(panels.Layout!.Windows!);

            await panels.ResetLayoutAsync();
            await WindowTestHelper.PumpAsync();

            Assert.Empty(panels.Layout!.Windows!);
            Assert.All(panels.Registry.Panels.Where(x => !x.AllowsMultipleInstances), x => Assert.True(panels.IsVisible(x.Id), x.Id));
            Assert.Equal(DefaultPanelLayouts.Create(PanelLayoutKind.Normal).EnumeratePanels().Select(x => x.ToString()).Order(), panels.Snapshot().EnumeratePanels().Select(x => x.ToString()).Order());
            Assert.All(fixture.Main.ViewMenu.Items.Where(x => x.IsCheckable), x => Assert.True(x.IsChecked));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task APanelOfAPluginThatLeavesKeepsItsPlaceForItsReturn()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var owner = new object();
        try
        {
            var panels = fixture.Main.Panels;
            var descriptor = new PanelDescriptor
            {
                Id = "Tests.Notes",
                Title = () => "Notes",
                DefaultPlacement = PanelPlacement.Bottom,
                DefaultGroup = DefaultPanelLayouts.BottomGroup,
                CreateViewModel = context => new NotesPanelViewModel(context),
            };
            panels.Registry.Register(descriptor, owner);
            await WindowTestHelper.PumpAsync();
            var notes = panels.FindDockable("Tests.Notes")!;
            Assert.Equal(DefaultPanelLayouts.BottomGroup, Assert.IsAssignableFrom<IDock>(notes.Owner).Id);
            Assert.Contains(fixture.Main.ViewMenu.Items, x => x.PanelId == "Tests.Notes");

            // The plugin leaves: its panel goes, and the saved layout keeps its place
            panels.Registry.UnregisterOwner(owner);
            await WindowTestHelper.PumpAsync();
            Assert.Null(panels.FindDockable("Tests.Notes"));
            Assert.True(((NotesPanelViewModel)notes.ViewModel).WasDestroyed);
            Assert.DoesNotContain(fixture.Main.ViewMenu.Items, x => x.PanelId == "Tests.Notes");
            panels.SaveLayout();
            var saved = GameStudioInternalSettings.CreateSessionPanelLayouts().Load(fixture.PackagePath, PanelLayoutKind.Normal)!;
            Assert.Equal(DefaultPanelLayouts.BottomGroup, saved.FindGroupOf(new PanelRef("Tests.Notes"))!.Id);
        }
        finally
        {
            fixture.Main.Panels.Registry.UnregisterOwner(owner);
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AFloatingPanelKeepsItsViewAndTheShortcutsOfTheMainWindow()
    {
        using var fixture = await ShellSessionFixture.OpenExecutableAsync(Platform.Type);
        var (builder, _) = fixture.ReplaceGameBuilder();
        fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var panels = fixture.Main.Panels;
            var explorer = Assert.Single(window.GetVisualDescendants().OfType<SolutionExplorerView>());

            panels.Factory.FloatDockable(panels.FindDockable(BuiltInPanels.SolutionExplorer)!);
            await WindowTestHelper.PumpAsync();
            var floating = Assert.Single(panels.Layout!.Windows!);
            var host = Assert.IsType<StrideHostWindow>(floating.Host);
            await WindowTestHelper.WaitUntilAsync(() => host.GetVisualDescendants().OfType<SolutionExplorerView>().Any(), "the panel in its window");
            // The same view, with its tree as the user left it
            Assert.Same(explorer, host.GetVisualDescendants().OfType<SolutionExplorerView>().Single());
            Assert.Empty(window.GetVisualDescendants().OfType<SolutionExplorerView>());

            // The shortcuts of the main window: F6 builds, and Undo falls back on the session's
            host.KeyPress(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            host.KeyRelease(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            await WindowTestHelper.WaitUntilAsync(() => builder.Requests.Count == 1 && !fixture.Main.Build.BuildInProgress, "the build");
            Assert.Contains(EditCommands.GetBindings(host), x => x.Kind == EditCommandKind.Undo && ReferenceEquals(x.Command, fixture.Main.UndoCommand));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static async Task<T> OpenEditorAsync<T>(ShellSessionFixture fixture, Stride.Core.Assets.Presentation.ViewModels.AssetViewModel asset)
        where T : Stride.Core.Assets.Editor.ViewModels.AssetEditorViewModel
    {
        fixture.Session.AssetCollection.SelectAssets([asset]);
        await WindowTestHelper.PumpAsync();
        fixture.Session.EditSelectedContentCommand.Execute();
        await WindowTestHelper.WaitUntilAsync(() => fixture.Session.EditorCollection.ActiveEditor?.Asset == asset, "the editor");
        await WindowTestHelper.PumpAsync();
        return Assert.IsType<T>(fixture.Session.EditorCollection.ActiveEditor);
    }

    /// <summary>
    /// The panel of a plugin.
    /// </summary>
    private sealed class NotesPanelViewModel(PanelContext context) : PanelViewModel(context)
    {
        public bool WasDestroyed { get; private set; }

        public override void Destroy()
        {
            WasDestroyed = true;
            base.Destroy();
        }
    }
}
