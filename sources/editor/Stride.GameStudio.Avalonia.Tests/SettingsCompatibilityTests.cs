// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Commands;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Editor.Settings;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Assets.Editor.SceneEditor;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Settings;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Both editors read and write GameStudioSettings.conf and GameStudioInternal.conf: a file the WPF Game Studio wrote loads in
/// this editor, which reads the settings they share and writes back, as they were, those it does not know.
/// </summary>
public sealed class SettingsCompatibilityTests : IDisposable
{
    private const string OpenedAsset = "bf3d2e8b-e681-4d42-88b2-9b219d8c9a07";

    // Written by the WPF Game Studio: its own keys (theme, UI editor, window, panels, docking layouts) next to the shared ones
    private const string WpfSettings = """
        !SettingsFile
        Settings:
            ExternalTools/ShaderEditor: !file C:/Tools/ShaderEdit.exe
            Interface/AskBeforeDeletingAssets: false
            Interface/Language: French
            Remote/Credentials/Host: build-machine
            Remote/Credentials/Port: 2222
            SceneEditor/KeyBindings/MoveCamForward: Z
            SceneEditor/KeyBindings/SnapSelectionToGrid: G
            SceneEditor/ViewportSettings/DisplayDirectionNames: true
            ScriptEditor/FontSize: 17
            Themes/ThemeName: DarkSteel
            UIEditor/AskBeforeDeletingUIElements: false
        """;

    private const string WpfInternalSettings = $"""
        !SettingsFile
        Settings:
            Internal/AssetPreviewPanelVisible: false
            Internal/MostRecentlyUsedSessionsData2:
                -   FilePath: !file C:/Projects/MyGame/MyGame.sln
                    Timestamp: 639289416265353646
                    OpenedAssets:
                        - {OpenedAsset}
                    DockingLayoutVersion: 9
                    DockingLayout: <LayoutRoot><RootPanel Orientation="Horizontal" /></LayoutRoot>
                    DockingLayoutEditors: <LayoutRoot><RootPanel Orientation="Vertical" /></LayoutRoot>
            Internal/WindowHeight: 777
            Internal/WindowMaximized: false
            Internal/WindowWidth: 1234
        """;

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.GameStudio.Avalonia.Tests", Guid.NewGuid().ToString("N"));

    public SettingsCompatibilityTests()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }

    [AvaloniaFact]
    public void TheSettingsOfTheWpfEditorLoadAndItsOwnStayAsTheyAre()
    {
        var path = Path.Combine(directory, "GameStudioSettings.conf");
        File.WriteAllText(path, WpfSettings);

        var container = SharedEditorSettings.SettingsContainer;
        // The scene editor's plugin registers them when it joins a session and takes them out when it leaves (another test)
        foreach (var key in EditorSettingsAttribute.GetKeys(typeof(SceneEditorSettings)))
            container.RegisterSettingsKey(key);
        var profile = container.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(profile);
        // The shared settings: the editor's own, the external tools, the remote host, the scene and script editors
        Assert.False(EditorSettings.AskBeforeDeletingAssets.GetValue(profile, true));
        // The language has the values of the WPF editor, which this editor maps to the same cultures
        Assert.Equal(SupportedLanguage.French, EditorSettings.Language.GetValue(profile, true));
        Assert.Equal("fr-FR", EditorSettings.Language.GetValue(profile, true).GetCulture().Name);
        Assert.Equal(CultureInfo.InstalledUICulture, SupportedLanguage.MachineDefault.GetCulture());
        Assert.Same(ExternalToolsSettings.ShaderEditor, EditorSettings.ShaderEditor);
        Assert.Equal(new UFile("C:/Tools/ShaderEdit.exe"), ExternalToolsSettings.ShaderEditor.GetValue(profile, true));
        Assert.Equal("build-machine", RemoteSettings.Host.GetValue(profile, true));
        Assert.Equal(2222, RemoteSettings.Port.GetValue(profile, true));
        Assert.Equal(Stride.Input.Keys.Z, SceneEditorSettings.MoveCamForward.GetValue(profile, true));
        Assert.True(SceneEditorSettings.DisplayDirectionNames.GetValue(profile, true));
        Assert.Equal(Stride.Input.Keys.G, SceneEditorSettings.SnapSelectionToGrid.GetValue(profile, true));

        // This editor changes a shared setting and saves
        RemoteSettings.Host.SetValue("other-machine", profile);
        Assert.True(container.SaveSettingsProfile(profile, path));

        var saved = File.ReadAllText(path);
        Assert.Contains("Remote/Credentials/Host: other-machine", saved);
        Assert.Contains("ScriptEditor/FontSize: 17", saved);
        // The settings of the WPF editor only
        Assert.Contains("Themes/ThemeName: DarkSteel", saved);
        Assert.Contains("UIEditor/AskBeforeDeletingUIElements: false", saved);
    }

    [AvaloniaFact]
    public void TheInternalSettingsOfTheWpfEditorLoadAndItsWindowAndLayoutsStayAsTheyAre()
    {
        var path = Path.Combine(directory, "GameStudioInternal.conf");
        File.WriteAllText(path, WpfInternalSettings);

        var container = InternalSettings.SettingsContainer;
        // A profile that isn't registered in the container doesn't get the keys registered after it loads: the static constructor
        // registers them, whatever test ran before
        RegisterKeys(typeof(GameStudioInternalSettings));
        var profile = container.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(profile);
        // What a session kept: its editors open again here
        var data = Assert.Single(RecentSessionsSettings.MostRecentlyUsedSessionsData.GetValue(profile, true));
        Assert.Equal(new UFile("C:/Projects/MyGame/MyGame.sln"), data.FilePath);
        Assert.Equal([AssetId.Parse(OpenedAsset)], data.OpenedAssets);
        // The window of the WPF editor is not this editor's
        Assert.Equal(GameStudioInternalSettings.NoPosition, GameStudioInternalSettings.WindowLeft.GetValue(profile, true));
        Assert.False(GameStudioInternalSettings.WindowMaximized.GetValue(profile, true));

        // This editor saves its window, its panels and the editors it had open
        GameStudioInternalSettings.WindowWidth.SetValue(900, profile);
        GameStudioInternalSettings.WindowMaximized.SetValue(true, profile);
        var layouts = new SessionPanelLayouts { FilePath = data.FilePath, Timestamp = 1, Normal = DefaultPanelLayouts.Create(PanelLayoutKind.Normal) };
        GameStudioInternalSettings.SessionLayouts.SetValue([layouts], profile);
        data.OpenedAssets.Clear();
        RecentSessionsSettings.MostRecentlyUsedSessionsData.SetValue([data], profile);
        Assert.True(container.SaveSettingsProfile(profile, path));

        var saved = File.ReadAllText(path);
        Assert.Contains("Internal/Avalonia/WindowWidth: 900", saved);
        Assert.Contains("Internal/Avalonia/WindowMaximized: true", saved);
        Assert.Contains("Internal/Avalonia/SessionLayouts:", saved);
        Assert.Contains("Stride.SolutionExplorer", saved);
        // The WPF editor finds its window, its panels and its docking layouts as it left them
        Assert.Contains("Internal/WindowWidth: 1234", saved);
        Assert.Contains("Internal/WindowHeight: 777", saved);
        Assert.Contains("Internal/WindowMaximized: false", saved);
        Assert.Contains("Internal/AssetPreviewPanelVisible: false", saved);
        Assert.Contains("DockingLayoutVersion: 9", saved);
        Assert.Contains("""DockingLayout: <LayoutRoot><RootPanel Orientation="Horizontal" /></LayoutRoot>""", saved);
        Assert.Contains("""DockingLayoutEditors: <LayoutRoot><RootPanel Orientation="Vertical" /></LayoutRoot>""", saved);

        // And this editor reads back what it wrote
        var reloaded = container.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(reloaded);
        Assert.Equal(900, GameStudioInternalSettings.WindowWidth.GetValue(reloaded, true));
        var reloadedLayouts = Assert.Single(GameStudioInternalSettings.SessionLayouts.GetValue(reloaded, true));
        Assert.Equal(data.FilePath, reloadedLayouts.FilePath);
        Assert.Equal(DefaultPanelLayouts.Create(PanelLayoutKind.Normal).EnumeratePanels(), reloadedLayouts.Normal!.EnumeratePanels());
        Assert.Null(reloadedLayouts.Editors);
        Assert.Empty(Assert.Single(RecentSessionsSettings.MostRecentlyUsedSessionsData.GetValue(reloaded, true)).OpenedAssets);
    }

    [AvaloniaFact]
    public async Task TheSettingsWindowShowsTheSharedCategoriesAndTheResetLayoutButton()
    {
        // The settings of the scene and script editors come with their plugin, which joins the session
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new Stride.Assets.Sprite.SpriteSheetAsset()));
        var serviceProvider = fixture.ServiceProvider;
        var window = new SettingsWindow { DataContext = new SettingsViewModel(serviceProvider, EditorSettings.SettingsContainer.CurrentProfile) };
        var previousCommand = EditorSettings.ResetEditorLayout.Command;
        var resets = 0;
        EditorSettings.ResetEditorLayout.Command = new AnonymousCommand(serviceProvider, () => resets++);
        await window.ShowAsync();
        try
        {
            var viewModel = Assert.IsType<SettingsViewModel>(window.DataContext);
            var names = viewModel.Categories.Select(x => x.Name).ToList();
            // The shared categories but Themes (roadmap row 16), UI editor (no Avalonia UI editor yet) and Test (DEBUG builds)
            Assert.Contains(ExternalToolsSettings.ExternalTools, names);
            Assert.Contains(RemoteSettings.Remote, names);
            Assert.Contains(SceneEditorSettings.SceneEditor, names);
            Assert.Contains("Script editor", names);
            Assert.Contains("Interface", names);

            // Interface > Reset Game Studio layout is a button
            viewModel.SelectedCategory = viewModel.Categories.Single(x => x.Name == "Interface");
            Button? reset = null;
            await WindowTestHelper.WaitUntilAsync(() => (reset = window.GetVisualDescendants().OfType<Button>().FirstOrDefault(x => Equals(x.Content, "Reset"))) is not null, "the Reset button");
            await window.ClickAsync(reset!);
            Assert.Equal(1, resets);

            // A whole number shows its value (the number box clamped it to 0 without the range of its type)
            viewModel.SelectedCategory = viewModel.Categories.Single(x => x.Name == RemoteSettings.Remote);
            NumericTextBox? port = null;
            await WindowTestHelper.WaitUntilAsync(() => (port = window.GetVisualDescendants().OfType<NumericTextBox>().FirstOrDefault()) is not null, "the Port box");
            await WindowTestHelper.PumpAsync();
            Assert.Equal(RemoteSettings.Port.GetValue(), (int?)port!.Value);
        }
        finally
        {
            EditorSettings.ResetEditorLayout.Command = previousCommand;
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public void SavingTheInternalSettingsKeepsTheRecentSessionsAnotherEditorWrote()
    {
        var previousSessions = RecentSessionsSettings.MostRecentlyUsedSessions.GetValue();
        var previousDirectory = InternalSettings.FileDialogLastImportDirectory.GetValue();
        try
        {
            // Another editor instance adds a session to the file
            var other = InternalSettings.LoadProfileCopy();
            var sessions = new MRUDictionary { ["4.5"] = [new MostRecentlyUsedFile(new UFile("C:/Projects/Other/Other.sln"))] };
            RecentSessionsSettings.MostRecentlyUsedSessions.SetValue(sessions, other);
            Assert.True(InternalSettings.SettingsContainer.SaveSettingsProfile(other, EditorPath.InternalConfigPath));

            // This one, which read the file before, saves another internal setting
            RecentSessionsSettings.MostRecentlyUsedSessions.SetValue(new MRUDictionary());
            InternalSettings.FileDialogLastImportDirectory.SetValue("C:/Imports");
            InternalSettings.Save();

            var saved = InternalSettings.LoadProfileCopy();
            Assert.Equal("C:/Imports", InternalSettings.FileDialogLastImportDirectory.GetValue(saved, true));
            var file = Assert.Single(Assert.Single(RecentSessionsSettings.MostRecentlyUsedSessions.GetValue(saved, true)).Value);
            Assert.Equal(new UFile("C:/Projects/Other/Other.sln"), file.FilePath);
            Assert.Single(Assert.Single(RecentSessionsSettings.MostRecentlyUsedSessions.GetValue()).Value);
        }
        finally
        {
            RecentSessionsSettings.MostRecentlyUsedSessions.SetValue(previousSessions);
            InternalSettings.FileDialogLastImportDirectory.SetValue(previousDirectory);
            InternalSettings.SaveProfile();
        }
    }

    [AvaloniaFact]
    public void TheWidgetsOfAScenePassThroughTheWpfEditorAsTheyAre()
    {
        var path = Path.Combine(directory, "MyGame.sdpkg.user");
        var sceneId = AssetId.New();
        var otherSceneId = AssetId.New();

        // This editor writes the scene settings, which both editors share, and the scene view widgets, under a key of their own
        var container = new SettingsContainer();
        var sceneSettings = new SettingsKey<Dictionary<AssetId, SceneSettingsData>>(PackageSceneSettings.SceneSettings.Name, container, () => new Dictionary<AssetId, SceneSettingsData>());
        var widgets = new SettingsKey<Dictionary<AssetId, Dictionary<string, bool>>>(PackageSceneViewWidgets.Visibility.Name, container, () => new Dictionary<AssetId, Dictionary<string, bool>>());
        var profile = container.CreateSettingsProfile(true);
        sceneSettings.SetValue(new Dictionary<AssetId, SceneSettingsData> { [sceneId] = new() { CamMoveSpeed = 3 } }, profile);
        widgets.SetValue(new Dictionary<AssetId, Dictionary<string, bool>>
        {
            [sceneId] = new() { ["Tests.Stats"] = false, ["Tests.Ruler"] = true },
            [otherSceneId] = new() { ["Tests.Stats"] = true },
        }, profile);
        Assert.True(container.SaveSettingsProfile(profile, path));
        Assert.Contains("Package/SceneViewWidgets", File.ReadAllText(path));

        // The WPF editor knows the scene settings only: it changes them and saves
        var wpfContainer = new SettingsContainer();
        var wpfSceneSettings = new SettingsKey<Dictionary<AssetId, SceneSettingsData>>(PackageSceneSettings.SceneSettings.Name, wpfContainer, () => new Dictionary<AssetId, SceneSettingsData>());
        var wpfProfile = wpfContainer.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(wpfProfile);
        var wpfValue = new Dictionary<AssetId, SceneSettingsData>(wpfSceneSettings.GetValue(wpfProfile, true));
        Assert.Equal(3, wpfValue[sceneId].CamMoveSpeed);
        wpfValue[sceneId].CamMoveSpeed = 5;
        wpfSceneSettings.SetValue(wpfValue, wpfProfile);
        Assert.True(wpfContainer.SaveSettingsProfile(wpfProfile, path));

        // This editor reads both back: the widgets as it left them
        var reloadedContainer = new SettingsContainer();
        var reloadedSceneSettings = new SettingsKey<Dictionary<AssetId, SceneSettingsData>>(PackageSceneSettings.SceneSettings.Name, reloadedContainer, () => new Dictionary<AssetId, SceneSettingsData>());
        var reloadedWidgets = new SettingsKey<Dictionary<AssetId, Dictionary<string, bool>>>(PackageSceneViewWidgets.Visibility.Name, reloadedContainer, () => new Dictionary<AssetId, Dictionary<string, bool>>());
        var reloaded = reloadedContainer.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(reloaded);
        Assert.Equal(5, reloadedSceneSettings.GetValue(reloaded, true)[sceneId].CamMoveSpeed);
        var visibility = reloadedWidgets.GetValue(reloaded, true);
        Assert.Equal(new Dictionary<string, bool> { ["Tests.Stats"] = false, ["Tests.Ruler"] = true }, visibility[sceneId]);
        Assert.Equal(new Dictionary<string, bool> { ["Tests.Stats"] = true }, visibility[otherSceneId]);
    }

    [AvaloniaFact]
    public void ThePluginsSwitchedOffPassThroughAWpfEditorThatDoesNotKnowThemAsTheyAre()
    {
        var path = Path.Combine(directory, "GameStudioInternal.conf");
        File.WriteAllText(path, WpfInternalSettings);
        var game = new UFile("C:/Projects/MyGame/MyGame.sln");

        // This editor switches a plugin off for a session, under a key of its own, which both editors of this version read
        var container = new SettingsContainer();
        var key = new SettingsKey<List<DisabledEditorCompanions>>(InternalSettings.DisabledEditorCompanions.Name, container, () => new List<DisabledEditorCompanions>());
        // The store writes the editor's current profile, as InternalSettings has
        var profile = container.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(profile);
        container.CurrentProfile = profile;
        var store = new DisabledEditorCompanionStore(() => profile, key, () => Assert.True(container.SaveSettingsProfile(profile, path)));
        store.Set(game, "MyPlugin.Editor", switchedOff: true);
        Assert.Contains("Internal/DisabledEditorCompanions:", File.ReadAllText(path));

        // A WPF editor of an earlier version knows its window only: it changes it and saves
        var wpfContainer = new SettingsContainer();
        var wpfWidth = new SettingsKey<int>("Internal/WindowWidth", wpfContainer, 0);
        var wpfProfile = wpfContainer.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(wpfProfile);
        wpfWidth.SetValue(1300, wpfProfile);
        Assert.True(wpfContainer.SaveSettingsProfile(wpfProfile, path));

        // Both read the entry back as it was written
        var reloadedContainer = new SettingsContainer();
        var reloadedKey = new SettingsKey<List<DisabledEditorCompanions>>(InternalSettings.DisabledEditorCompanions.Name, reloadedContainer, () => new List<DisabledEditorCompanions>());
        var reloaded = reloadedContainer.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(reloaded);
        var entry = Assert.Single(reloadedKey.GetValue(reloaded, true));
        Assert.Equal(game, entry.FilePath);
        Assert.Equal(["MyPlugin.Editor"], entry.Packages);
        var saved = File.ReadAllText(path);
        Assert.Contains("Internal/WindowWidth: 1300", saved);
        Assert.Contains("DockingLayoutVersion: 9", saved);
    }

    [AvaloniaFact]
    public void TheExtensionsPassThroughAWpfEditorThatDoesNotKnowThemAsTheyAre()
    {
        var path = Path.Combine(directory, "GameStudioInternal.conf");
        File.WriteAllText(path, WpfInternalSettings);
        var editorVersion = new Version(4, 5);

        // This editor installs an extension and switches another off, under a key of its own, which both editors read
        var container = new SettingsContainer();
        var key = new SettingsKey<List<EditorExtensionList>>(InternalSettings.ExtensionPackages.Name, container, () => new List<EditorExtensionList>());
        var profile = container.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(profile);
        container.CurrentProfile = profile;
        var store = new EditorExtensionStore(() => profile, key, () => Assert.True(container.SaveSettingsProfile(profile, path)));
        store.Set(editorVersion, new EditorExtension { Id = "MyTool.Editor", Version = "1.2.0" });
        store.Set(editorVersion, new EditorExtension { Id = "MyViews.Editor.Avalonia", Version = "0.3.0", Toolkit = "Avalonia", IsOff = true });
        Assert.Contains("Internal/EditorExtensions:", File.ReadAllText(path));

        // A WPF editor of an earlier version knows its window only: it changes it and saves
        var wpfContainer = new SettingsContainer();
        var wpfWidth = new SettingsKey<int>("Internal/WindowWidth", wpfContainer, 0);
        var wpfProfile = wpfContainer.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(wpfProfile);
        wpfWidth.SetValue(1300, wpfProfile);
        Assert.True(wpfContainer.SaveSettingsProfile(wpfProfile, path));

        // Both read the list back as it was written
        var reloadedContainer = new SettingsContainer();
        var reloadedKey = new SettingsKey<List<EditorExtensionList>>(InternalSettings.ExtensionPackages.Name, reloadedContainer, () => new List<EditorExtensionList>());
        var reloaded = reloadedContainer.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(reloaded);
        var list = Assert.Single(reloadedKey.GetValue(reloaded, true));
        Assert.Equal("4.5", list.EditorVersion);
        Assert.Equal([("MyTool.Editor", "1.2.0", (string?)null, false), ("MyViews.Editor.Avalonia", "0.3.0", "Avalonia", true)],
            list.Packages.Select(x => (x.Id, x.Version, x.Toolkit, x.IsOff)));
        var saved = File.ReadAllText(path);
        Assert.Contains("Internal/WindowWidth: 1300", saved);
        Assert.Contains("DockingLayoutVersion: 9", saved);
    }

    private static void RegisterKeys(params Type[] settingsTypes)
    {
        foreach (var type in settingsTypes)
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
    }
}
