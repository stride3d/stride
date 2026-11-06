// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Panels;
using Stride.Core.IO;
using Stride.Core.Settings;
using static Stride.Core.Assets.Editor.Tests.Panels.PanelTestHelpers;

namespace Stride.Core.Assets.Editor.Tests.Panels;

public sealed class TestSessionPanelLayoutStore : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "StridePanelLayouts", Guid.NewGuid().ToString("N"));
    private readonly SettingsContainer container = new();
    private readonly SettingsKey<List<SessionPanelLayouts>> key;
    private readonly SettingsProfile profile;

    public TestSessionPanelLayoutStore()
    {
        Directory.CreateDirectory(directory);
        key = new SettingsKey<List<SessionPanelLayouts>>("Internal/Avalonia/SessionLayouts", container, () => new List<SessionPanelLayouts>());
        profile = container.CreateSettingsProfile(true);
    }

    private string FilePath => Path.Combine(directory, "GameStudioInternal.conf");

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    [Fact]
    public void ALayoutIsSavedPerSessionAndKind()
    {
        var registry = CreateBuiltInRegistry();
        var game = new UFile("/Projects/Game/Game.sln");
        var other = new UFile("/Projects/Other/Other.sln");
        var normal = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry);
        normal.Hidden.Add(BuiltInPanels.References);
        var editors = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Editors, registry);

        CreateStore().Save(game, PanelLayoutKind.Normal, normal);
        CreateStore().Save(game, PanelLayoutKind.Editors, editors);
        CreateStore().Save(other, PanelLayoutKind.Normal, PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry));

        var store = CreateStore();
        Assert.Equal([BuiltInPanels.References], store.Load(game, PanelLayoutKind.Normal)?.Hidden);
        Assert.Single(store.Load(game, PanelLayoutKind.Editors)!.EnumerateNodes().OfType<PanelDocuments>());
        Assert.Empty(store.Load(other, PanelLayoutKind.Normal)!.Hidden);
        Assert.Null(store.Load(other, PanelLayoutKind.Editors));
        // The path is compared as the editors do, whatever its case
        Assert.NotNull(store.Load(new UFile("/projects/game/GAME.sln"), PanelLayoutKind.Normal));

        // A reset forgets both layouts of that session only
        store.Reset(game);
        Assert.Null(store.Load(game, PanelLayoutKind.Normal));
        Assert.Null(store.Load(game, PanelLayoutKind.Editors));
        Assert.NotNull(store.Load(other, PanelLayoutKind.Normal));
    }

    [Fact]
    public void OnlyTheMostRecentSessionsAreKept()
    {
        var layout = new PanelLayout { Root = new PanelGroup { Panels = [new PanelRef("A")] } };
        var sessions = Enumerable.Range(0, SessionPanelLayoutStore.MaxSessionCount).Select(x => new UFile($"/Projects/Game{x}/Game{x}.sln")).ToList();
        foreach (var session in sessions)
            CreateStore().Save(session, PanelLayoutKind.Normal, layout);

        // The oldest one is used again, then a new one comes
        CreateStore().Save(sessions[0], PanelLayoutKind.Editors, layout);
        CreateStore().Save(new UFile("/Projects/Latest/Latest.sln"), PanelLayoutKind.Normal, layout);

        var store = CreateStore();
        Assert.NotNull(store.Load(sessions[0], PanelLayoutKind.Normal));
        Assert.Null(store.Load(sessions[1], PanelLayoutKind.Normal));
        Assert.Equal(SessionPanelLayoutStore.MaxSessionCount, key.GetValue(profile, true).Count);
    }

    [Fact]
    public void TwoEditorsKeepEachOtherSessions()
    {
        var layout = new PanelLayout { Root = new PanelGroup { Panels = [new PanelRef("A")] } };
        // Each editor saves into its own profile, and reads the file before each change
        var first = CreateEditorStore();
        var second = CreateEditorStore();

        first.Save(new UFile("/Projects/First/First.sln"), PanelLayoutKind.Normal, layout);
        second.Save(new UFile("/Projects/Second/Second.sln"), PanelLayoutKind.Normal, layout);
        first.Save(new UFile("/Projects/First/First.sln"), PanelLayoutKind.Editors, layout);

        var reader = CreateEditorStore();
        Assert.NotNull(reader.Load(new UFile("/Projects/First/First.sln"), PanelLayoutKind.Editors));
        Assert.NotNull(reader.Load(new UFile("/Projects/Second/Second.sln"), PanelLayoutKind.Normal));
    }

    [Fact]
    public void TheLayoutsGoThroughTheSettingsFile()
    {
        var registry = CreateBuiltInRegistry();
        registry.Register(Panel("Plugin.Multi", multiple: true));
        var layout = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Editors, registry);
        layout.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Panels.Add(new PanelRef("Plugin.Multi", "first"));
        layout.Floating.Add(new FloatingPanelWindow { Root = new PanelGroup { Panels = [new PanelRef("Plugin.Multi", "second")] }, X = -1200.5, Y = 10, Width = 640, Height = 480 });
        layout.Pinned.Add(new PinnedPanelGroup { Side = PanelSide.Left, Panels = [new PanelRef(BuiltInPanels.ActionHistory)] });
        layout.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Panels.Remove(new PanelRef(BuiltInPanels.ActionHistory));
        layout.Hidden.Add(BuiltInPanels.AssetLog);
        var session = new UFile("/Projects/Game/Game.sln");

        var store = CreateEditorStore();
        store.Save(session, PanelLayoutKind.Editors, layout);

        var text = File.ReadAllText(FilePath);
        Assert.Contains("Internal/Avalonia/SessionLayouts:", text);
        Assert.Contains("!PanelGroup", text);
        Assert.Contains("Stride.RightBottom", text);

        var reloaded = CreateEditorStore().Load(session, PanelLayoutKind.Editors);
        Assert.NotNull(reloaded);
        Assert.Equal(PanelLayoutFallback.None, PanelLayoutResolver.Resolve(reloaded, PanelLayoutKind.Editors, registry).Fallback);
        Assert.Equal(Describe(layout), Describe(reloaded));
    }

    // Every node, depth first, with its panels and proportions
    private static List<string> Describe(PanelLayout layout)
    {
        var lines = new List<string> { $"v{layout.Version}" };
        lines.AddRange(layout.EnumerateNodes().Select(x => x switch
        {
            PanelSplit split => $"split {split.Orientation} {string.Join(",", split.Proportions)}",
            PanelGroup group => $"group {group.Id} [{string.Join(",", group.Panels)}] {group.Active}",
            _ => "documents",
        }));
        lines.AddRange(layout.Floating.Select(x => $"floating {x.X},{x.Y},{x.Width},{x.Height}"));
        lines.AddRange(layout.Pinned.Select(x => $"pinned {x.Side} [{string.Join(",", x.Panels)}]"));
        lines.Add($"hidden [{string.Join(",", layout.Hidden)}]");
        return lines;
    }

    private SessionPanelLayoutStore CreateStore()
    {
        // The profile stands for the file: what was saved is what the next store loads
        return new SessionPanelLayoutStore(() => profile, key, () => { });
    }

    // A store over the file, as an editor has: its own settings, the file read before each change and written after it
    private SessionPanelLayoutStore CreateEditorStore()
    {
        var editorContainer = new SettingsContainer();
        var editorKey = new SettingsKey<List<SessionPanelLayouts>>(key.Name, editorContainer, () => new List<SessionPanelLayouts>());
        var current = editorContainer.LoadSettingsProfile(FilePath, true) ?? editorContainer.CreateSettingsProfile(true);
        return new SessionPanelLayoutStore(
            () => editorContainer.LoadSettingsProfile(FilePath, false, null, false) ?? editorContainer.CreateSettingsProfile(false, null, false),
            editorKey,
            () => Assert.True(editorContainer.SaveSettingsProfile(current, FilePath)));
    }
}
