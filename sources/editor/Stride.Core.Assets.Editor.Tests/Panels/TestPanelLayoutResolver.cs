// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Panels;
using static Stride.Core.Assets.Editor.Tests.Panels.PanelTestHelpers;

namespace Stride.Core.Assets.Editor.Tests.Panels;

public sealed class TestPanelLayoutResolver
{
    [Fact]
    public void TheDefaultLayoutsPlaceThePanelsAsWpf()
    {
        var registry = CreateBuiltInRegistry();

        var normal = PanelLayoutResolver.Resolve(null, PanelLayoutKind.Normal, registry);
        Assert.Equal(PanelLayoutFallback.NotSaved, normal.Fallback);
        Assert.Equal([BuiltInPanels.SolutionExplorer], Ids(normal.Layout.FindGroup(DefaultPanelLayouts.LeftGroup)));
        Assert.Equal([BuiltInPanels.AssetView], Ids(normal.Layout.FindGroup(DefaultPanelLayouts.CenterGroup)));
        Assert.Equal([BuiltInPanels.References, BuiltInPanels.AssetLog, BuiltInPanels.BuildLog], Ids(normal.Layout.FindGroup(DefaultPanelLayouts.BottomGroup)));
        Assert.Equal(new PanelRef(BuiltInPanels.AssetLog), normal.Layout.FindGroup(DefaultPanelLayouts.BottomGroup)!.Active);
        Assert.Equal([BuiltInPanels.PropertyGrid], Ids(normal.Layout.FindGroup(DefaultPanelLayouts.RightTopGroup)));
        Assert.Equal([BuiltInPanels.AssetPreview, BuiltInPanels.ActionHistory], Ids(normal.Layout.FindGroup(DefaultPanelLayouts.RightBottomGroup)));
        Assert.Empty(normal.Layout.EnumerateNodes().OfType<PanelDocuments>());
        Assert.Empty(normal.Layout.Hidden);

        var editors = PanelLayoutResolver.Resolve(null, PanelLayoutKind.Editors, registry);
        Assert.Single(editors.Layout.EnumerateNodes().OfType<PanelDocuments>());
        Assert.Equal([BuiltInPanels.SolutionExplorer], Ids(editors.Layout.FindGroup(DefaultPanelLayouts.BottomLeftGroup)));
        Assert.Equal([BuiltInPanels.AssetView, BuiltInPanels.AssetLog, BuiltInPanels.BuildLog], Ids(editors.Layout.FindGroup(DefaultPanelLayouts.BottomRightGroup)));
        Assert.Equal([BuiltInPanels.AssetPreview, BuiltInPanels.ActionHistory, BuiltInPanels.References], Ids(editors.Layout.FindGroup(DefaultPanelLayouts.RightBottomGroup)));
        Assert.Equal(new PanelRef(BuiltInPanels.References), editors.Layout.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Active);

        // Every panel once in each layout
        foreach (var layout in new[] { normal.Layout, editors.Layout })
            Assert.Equal(BuiltInIds.Order(), layout.EnumeratePanels().Select(x => x.PanelId).Order());
    }

    [Fact]
    public void ASavedLayoutIsUsedAsItIs()
    {
        var registry = CreateBuiltInRegistry();
        var saved = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry);
        // The user moved the edit history to its own floating window and hid the references
        saved.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Panels.Remove(new PanelRef(BuiltInPanels.ActionHistory));
        saved.Floating.Add(new FloatingPanelWindow { Root = new PanelGroup { Panels = [new PanelRef(BuiltInPanels.ActionHistory)] }, X = 10, Y = 20, Width = 300, Height = 200 });
        saved.Hidden.Add(BuiltInPanels.References);

        var resolved = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry);

        Assert.Equal(PanelLayoutFallback.None, resolved.Fallback);
        Assert.NotSame(saved, resolved.Layout);
        Assert.Equal([BuiltInPanels.AssetPreview], Ids(resolved.Layout.FindGroup(DefaultPanelLayouts.RightBottomGroup)));
        var window = Assert.Single(resolved.Layout.Floating);
        Assert.Equal([BuiltInPanels.ActionHistory], Ids(window.Root as PanelGroup));
        Assert.Equal((10, 20, 300, 200), (window.X, window.Y, window.Width, window.Height));
        // A hidden panel keeps its place
        Assert.Equal([BuiltInPanels.References], resolved.Layout.Hidden);
        Assert.Contains(new PanelRef(BuiltInPanels.References), resolved.Layout.FindGroup(DefaultPanelLayouts.BottomGroup)!.Panels);
    }

    [Fact]
    public void APanelHiddenWhenTheSessionOpensIsHiddenInItsPlaceOnlyThen()
    {
        var registry = CreateBuiltInRegistry();
        registry.Register(new PanelDescriptor
        {
            Id = "Tests.Curves",
            Title = () => "Curves",
            DefaultPlacement = PanelPlacement.Document,
            HiddenWhenSessionOpens = true,
            CreateViewModel = _ => throw new NotSupportedException(),
        });
        // The session closed with the panel shown, next to the asset view
        var saved = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry);
        Assert.Empty(saved.Hidden);

        var opening = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry, sessionOpening: true);
        var later = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry);

        Assert.Equal(["Tests.Curves"], opening.Layout.Hidden);
        Assert.Equal([BuiltInPanels.AssetView, "Tests.Curves"], Ids(opening.Layout.FindGroup(DefaultPanelLayouts.CenterGroup)));
        Assert.Empty(later.Layout.Hidden);
    }

    [Theory]
    [InlineData(PanelLayout.CurrentVersion + 1, PanelLayoutFallback.OtherVersion)]
    [InlineData(0, PanelLayoutFallback.OtherVersion)]
    public void ALayoutOfAnotherVersionGivesTheDefaultOne(int version, PanelLayoutFallback expected)
    {
        var registry = CreateBuiltInRegistry();
        var saved = new PanelLayout { Version = version, Root = new PanelGroup { Panels = [new PanelRef(BuiltInPanels.AssetView)] } };

        var resolved = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry);

        Assert.Equal(expected, resolved.Fallback);
        Assert.Equal(PanelLayout.CurrentVersion, resolved.Layout.Version);
        Assert.NotNull(resolved.Layout.FindGroup(DefaultPanelLayouts.CenterGroup));
    }

    [Fact]
    public void ALayoutThatCannotBeUsedGivesTheDefaultOne()
    {
        var registry = CreateBuiltInRegistry();
        var withoutEditors = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry);
        var withEditors = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Editors, registry);

        // The layout with editors needs their area, the one without them must not have it
        Assert.Equal(PanelLayoutFallback.Invalid, PanelLayoutResolver.Resolve(withoutEditors, PanelLayoutKind.Editors, registry).Fallback);
        Assert.Equal(PanelLayoutFallback.Invalid, PanelLayoutResolver.Resolve(withEditors, PanelLayoutKind.Normal, registry).Fallback);
        // Twice is once too many
        var twice = withEditors.Clone();
        ((PanelSplit)twice.Root!).Children.Add(new PanelDocuments());
        ((PanelSplit)twice.Root!).Proportions.Add(1);
        Assert.Equal(PanelLayoutFallback.Invalid, PanelLayoutResolver.Resolve(twice, PanelLayoutKind.Editors, registry).Fallback);
        // Editors are not saved in floating windows
        var floatingEditors = withEditors.Clone();
        floatingEditors.Floating.Add(new FloatingPanelWindow { Root = new PanelDocuments(), Width = 100, Height = 100 });
        Assert.Equal(PanelLayoutFallback.Invalid, PanelLayoutResolver.Resolve(floatingEditors, PanelLayoutKind.Editors, registry).Fallback);
        // A node the settings file lost
        var broken = withoutEditors.Clone();
        ((PanelSplit)broken.Root!).Children.Add(null!);
        Assert.Equal(PanelLayoutFallback.Invalid, PanelLayoutResolver.Resolve(broken, PanelLayoutKind.Normal, registry).Fallback);
        broken = withoutEditors.Clone();
        broken.FindGroup(DefaultPanelLayouts.LeftGroup)!.Panels.Add(null!);
        Assert.Equal(PanelLayoutFallback.Invalid, PanelLayoutResolver.Resolve(broken, PanelLayoutKind.Normal, registry).Fallback);
    }

    [Fact]
    public void APanelNoLongerRegisteredIsLeftOutAndItsEmptyGroupDropped()
    {
        var registry = CreateBuiltInRegistry();
        registry.Register(Panel("Plugin.Tool"));
        var saved = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry);
        // The tool has a group of its own on the right
        var root = (PanelSplit)saved.Root!;
        Assert.Equal(["Plugin.Tool"], Ids(root.Children.Last() as PanelGroup));
        Assert.Equal(3, root.Children.Count);
        saved.Hidden.Add("Plugin.Tool");

        registry.Unregister("Plugin.Tool");
        var resolved = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry);

        Assert.Equal(PanelLayoutFallback.None, resolved.Fallback);
        Assert.DoesNotContain(resolved.Layout.EnumeratePanels(), x => x.PanelId == "Plugin.Tool");
        root = (PanelSplit)resolved.Layout.Root!;
        Assert.Equal(2, root.Children.Count);
        Assert.Equal(2, root.Proportions.Count);
        Assert.Empty(resolved.Layout.Hidden);
    }

    [Fact]
    public void ASplitLeftWithOneNodeIsReplacedByIt()
    {
        var registry = new PanelRegistry();
        registry.Register(Panel("A"));
        var saved = new PanelLayout
        {
            Root = new PanelSplit
            {
                Orientation = PanelOrientation.Horizontal,
                Children = [new PanelGroup { Panels = [new PanelRef("A")] }, new PanelGroup { Panels = [new PanelRef("Gone")] }],
                Proportions = [1, 1],
            },
        };

        var resolved = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry);

        Assert.Equal(["A"], Ids(resolved.Layout.Root as PanelGroup));
    }

    [Fact]
    public void ARegisteredPanelMissingFromTheLayoutGoesToItsDefaultPlace()
    {
        var registry = CreateBuiltInRegistry();
        var saved = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry);
        // A layout saved before these panels existed
        saved.FindGroup(DefaultPanelLayouts.BottomGroup)!.Panels.Remove(new PanelRef(BuiltInPanels.BuildLog));
        registry.Register(Panel("Plugin.InGroup", group: DefaultPanelLayouts.RightBottomGroup));
        registry.Register(Panel("Plugin.Left", PanelPlacement.Left, group: "Plugin.Tools"));
        registry.Register(Panel("Plugin.Left2", PanelPlacement.Left, group: "Plugin.Tools"));
        registry.Register(Panel("Plugin.Bottom", PanelPlacement.Bottom));
        registry.Register(Panel("Plugin.Floating", PanelPlacement.Floating));
        registry.Register(Panel("Plugin.Centre", PanelPlacement.Document));
        registry.Register(Panel("Plugin.Off", PanelPlacement.Right, visible: false));
        registry.Register(Panel("Plugin.Multi", multiple: true));

        var layout = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry).Layout;

        // Where the default layout has it
        Assert.Contains(new PanelRef(BuiltInPanels.BuildLog), layout.FindGroup(DefaultPanelLayouts.BottomGroup)!.Panels);
        // The group it names
        Assert.Contains(new PanelRef("Plugin.InGroup"), layout.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Panels);
        // A new group at its placement, which the panels of the same group share: here the left, then the bottom of the
        // whole window, then the right of it
        var root = (PanelSplit)layout.Root!;
        Assert.Equal(PanelOrientation.Horizontal, root.Orientation);
        Assert.Equal(["Plugin.Off"], Ids(root.Children[1] as PanelGroup));
        Assert.Equal(["Plugin.Off"], layout.Hidden);
        var vertical = Assert.IsType<PanelSplit>(root.Children[0]);
        Assert.Equal(PanelOrientation.Vertical, vertical.Orientation);
        Assert.Equal(["Plugin.Bottom"], Ids(vertical.Children[1] as PanelGroup));
        var main = Assert.IsType<PanelSplit>(vertical.Children[0]);
        Assert.Equal(["Plugin.Left", "Plugin.Left2"], Ids(main.Children[0] as PanelGroup));
        Assert.Equal("Plugin.Tools", ((PanelGroup)main.Children[0]).Id);
        Assert.Equal(main.Children.Count, main.Proportions.Count);
        Assert.Equal(["Plugin.Floating"], Ids(Assert.Single(layout.Floating).Root as PanelGroup));
        Assert.Contains(new PanelRef("Plugin.Centre"), layout.FindGroup(DefaultPanelLayouts.CenterGroup)!.Panels);
        // An instance opens only when asked
        Assert.DoesNotContain(layout.EnumeratePanels(), x => x.PanelId == "Plugin.Multi");

        // With editors, a centre panel goes with the asset view
        var editors = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Editors, registry);
        Assert.Contains(new PanelRef("Plugin.Centre"), editors.FindGroup(DefaultPanelLayouts.BottomRightGroup)!.Panels);
    }

    [Fact]
    public void InstancesAreKeptAndPanelsPlacedTwiceOnlyOnce()
    {
        var registry = new PanelRegistry();
        registry.Register(Panel("Multi", multiple: true));
        registry.Register(Panel("Single"));
        var saved = new PanelLayout
        {
            Root = new PanelGroup
            {
                Panels = [new PanelRef("Multi", "1"), new PanelRef("Multi", "2"), new PanelRef("Multi"), new PanelRef("Single"), new PanelRef("Single", "3"), new PanelRef("Multi", "1")],
                Active = new PanelRef("Single", "3"),
            },
            Pinned = [new PinnedPanelGroup { Side = PanelSide.Left, Panels = [new PanelRef("Single")] }],
            Hidden = ["Multi", "Single", "Single"],
        };

        var layout = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry).Layout;

        Assert.Equal(["Multi::1", "Multi::2", "Single"], Ids(layout.Root as PanelGroup));
        Assert.Null(((PanelGroup)layout.Root!).Active);
        Assert.Empty(layout.Pinned);
        Assert.Equal(["Single"], layout.Hidden);
    }

    [Fact]
    public void TheSavedLayoutKeepsThePlaceOfAPanelThatIsNotRegisteredNow()
    {
        var registry = CreateBuiltInRegistry();
        registry.Register(Panel("Plugin.Tool", group: DefaultPanelLayouts.RightBottomGroup));
        registry.Register(Panel("Plugin.Pinned"));
        registry.Register(Panel("Plugin.Alone", PanelPlacement.Left));
        var previous = PanelLayoutResolver.CreateDefault(PanelLayoutKind.Normal, registry);
        previous.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Panels.Remove(new PanelRef("Plugin.Tool"));
        previous.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Panels.Insert(1, new PanelRef("Plugin.Tool"));
        previous.EnumerateNodes().OfType<PanelGroup>().Single(x => x.Panels.Contains(new PanelRef("Plugin.Pinned"))).Panels.Clear();
        previous.Pinned.Add(new PinnedPanelGroup { Side = PanelSide.Bottom, Panels = [new PanelRef("Plugin.Pinned")] });
        previous.Hidden.Add("Plugin.Tool");

        // The plugin is off: the layout shown has none of its panels
        registry.Unregister("Plugin.Tool");
        registry.Unregister("Plugin.Pinned");
        registry.Unregister("Plugin.Alone");
        var shown = PanelLayoutResolver.Resolve(previous, PanelLayoutKind.Normal, registry).Layout;
        Assert.DoesNotContain(shown.EnumeratePanels(), x => x.PanelId.StartsWith("Plugin."));

        // The user moves the asset preview, then the layout is saved
        shown.FindGroup(DefaultPanelLayouts.RightBottomGroup)!.Panels.Reverse();
        var saved = PanelLayoutResolver.KeepUnavailablePanels(shown, previous, registry);

        Assert.Equal([BuiltInPanels.ActionHistory, "Plugin.Tool", BuiltInPanels.AssetPreview], Ids(saved.FindGroup(DefaultPanelLayouts.RightBottomGroup)));
        Assert.Equal(["Plugin.Pinned"], Ids(new PanelGroup { Panels = Assert.Single(saved.Pinned).Panels }));
        Assert.Equal(["Plugin.Tool"], saved.Hidden);
        // Its group is gone: it gets its default place when it returns
        Assert.DoesNotContain(saved.EnumeratePanels(), x => x.PanelId == "Plugin.Alone");
        // The layout shown is not changed
        Assert.DoesNotContain(shown.EnumeratePanels(), x => x.PanelId.StartsWith("Plugin."));

        // The plugin is back: its panels are where they were
        registry.Register(Panel("Plugin.Tool", group: DefaultPanelLayouts.RightBottomGroup));
        registry.Register(Panel("Plugin.Pinned"));
        var back = PanelLayoutResolver.Resolve(saved, PanelLayoutKind.Normal, registry).Layout;
        Assert.Equal([BuiltInPanels.ActionHistory, "Plugin.Tool", BuiltInPanels.AssetPreview], Ids(back.FindGroup(DefaultPanelLayouts.RightBottomGroup)));
        Assert.Equal(PanelSide.Bottom, Assert.Single(back.Pinned).Side);
        Assert.Equal(["Plugin.Tool"], back.Hidden);
    }

    [Fact]
    public void AFloatingWindowNoScreenShowsComesBackOnTheMainOne()
    {
        var layout = new PanelLayout
        {
            Floating =
            [
                // On the second screen, which is still there
                new FloatingPanelWindow { Root = new PanelGroup(), X = 2000, Y = 100, Width = 400, Height = 300 },
                // On a screen that was unplugged
                new FloatingPanelWindow { Root = new PanelGroup(), X = 4000, Y = 100, Width = 400, Height = 300 },
                // Its title bar above the screen
                new FloatingPanelWindow { Root = new PanelGroup(), X = 100, Y = -200, Width = 400, Height = 300 },
                // Bigger than the main screen
                new FloatingPanelWindow { Root = new PanelGroup(), X = -5000, Y = 0, Width = 3000, Height = 2000 },
            ],
        };
        var main = new PanelBounds(0, 0, 1920, 1040);

        Assert.True(PanelLayoutResolver.FitFloatingWindows(layout, [main, new PanelBounds(1920, 0, 1920, 1040)], main));

        Assert.Equal((2000, 100), (layout.Floating[0].X, layout.Floating[0].Y));
        Assert.Equal((760, 370, 400, 300), (layout.Floating[1].X, layout.Floating[1].Y, layout.Floating[1].Width, layout.Floating[1].Height));
        Assert.Equal((760, 370), (layout.Floating[2].X, layout.Floating[2].Y));
        Assert.Equal((0, 0, 1920, 1040), (layout.Floating[3].X, layout.Floating[3].Y, layout.Floating[3].Width, layout.Floating[3].Height));
        Assert.False(PanelLayoutResolver.FitFloatingWindows(layout, [main, new PanelBounds(1920, 0, 1920, 1040)], main));
    }
}
