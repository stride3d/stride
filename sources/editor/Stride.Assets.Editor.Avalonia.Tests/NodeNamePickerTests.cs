// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Models;
using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The node name of a node link (physics components, SpriteStudio node link) is picked in the list of the nodes of the
/// model, as with the WPF FilteringComboBox: the property changes once, when a node is picked or on Enter.
/// </summary>
public sealed class NodeNamePickerTests
{
    private static readonly NodeInformation[] Nodes =
    [
        new() { Name = "Root", Depth = 0 },
        new() { Name = "Spine", Depth = 1 },
        new() { Name = "Head", Depth = 2 },
    ];

    [AvaloniaFact]
    public void TheProviderMatchesTheNodeNameOfALinkWithNodes()
    {
        var provider = Provider();
        var holder = new LinkHolder();
        var context = new NodeViewModelContext(new AvailableNodesUpdater());

        Assert.True(provider.Match(context.MemberOf(holder, nameof(LinkHolder.Link)).GetChild(nameof(NodeLink.NodeName))!));
        // Without the nodes of a model, the name is a plain text
        Assert.False(provider.Match(new NodeViewModelContext().MemberOf(holder, nameof(LinkHolder.Link)).GetChild(nameof(NodeLink.NodeName))!));
        Assert.False(provider.Match(context.MemberOf(holder, nameof(LinkHolder.Name))));
        Assert.Equal("ModelNodeName", provider.Name);
    }

    [AvaloniaFact]
    public void TypingFiltersTheNodesAndEnterSetsTheNameOnce()
    {
        var (window, box, link) = Show();

        box.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.IsDropDownOpen);
        box.Text = string.Empty;
        window.KeyTextInput("he");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, link.Sets);
        Assert.Equal(["Head"], ShownNodes(box));

        window.KeyTextInput("ad");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("head", link.NodeName, ignoreCase: true);
        Assert.Equal(1, link.Sets);
        window.Close();
    }

    [AvaloniaFact]
    public void PickingANodeSetsTheName()
    {
        var (window, box, link) = Show();

        box.Focus();
        Dispatcher.UIThread.RunJobs();
        box.Text = string.Empty;
        window.KeyTextInput("Sp");
        Dispatcher.UIThread.RunJobs();
        box.SelectedItem = Nodes[1];
        box.IsDropDownOpen = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Spine", link.NodeName);
        Assert.Equal(1, link.Sets);
        window.Close();
    }

    [AvaloniaFact]
    public void EscapeShowsTheNameAgainWithoutChangingIt()
    {
        var (window, box, link) = Show();

        box.Focus();
        Dispatcher.UIThread.RunJobs();
        window.KeyTextInput("Spi");
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Root", box.Text);
        Assert.False(box.IsDropDownOpen);
        // Nothing to set when the focus leaves afterwards
        window.Close();
        Assert.Equal("Root", link.NodeName);
        Assert.Equal(0, link.Sets);
    }

    private static ModelNodeNameTemplateProvider Provider() => new EntityPropertyTemplateProviders().Values.OfType<ModelNodeNameTemplateProvider>().Single();

    private static (Window Window, AutoCompleteBox Box, NodeLink Link) Show()
    {
        var holder = new LinkHolder();
        var node = new NodeViewModelContext(new AvailableNodesUpdater()).MemberOf(holder, nameof(LinkHolder.Link)).GetChild(nameof(NodeLink.NodeName))!;
        var content = Provider().Template.Build(node)!;
        content.DataContext = node;
        // Another control to give the focus to
        var window = new Window { Width = 400, Height = 300, Content = new StackPanel { Children = { content, new TextBox() } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var box = window.GetVisualDescendants().OfType<AutoCompleteBox>().Single();
        Assert.Equal("Root", box.Text);
        return (window, box, holder.Link);
    }

    private static List<string> ShownNodes(AutoCompleteBox box)
    {
        // The list is in the popup of the box, out of its visual tree
        var popup = box.GetVisualDescendants().OfType<Popup>().Single();
        var list = Assert.IsAssignableFrom<Control>(popup.Child).GetSelfAndVisualDescendants().OfType<ListBox>().Single();
        return list.Items.OfType<NodeInformation>().Select(x => x.Name).ToList();
    }

    // What the node link updaters (PhysicsNodeLinkNodeUpdater, ModelNodeLinkNodeUpdater) set on the component
    private sealed class AvailableNodesUpdater : INodePresenterUpdater
    {
        public void UpdateNode(INodePresenter node)
        {
            if (node.Name == nameof(NodeLink.NodeName) && node.Parent is { } parent)
                parent.AttachedProperties.Set(ModelNodeLinkData.Key, Nodes);
        }

        public void FinalizeTree(INodePresenter root)
        {
        }
    }
}

public sealed class LinkHolder
{
    public string Name { get; set; } = "link";

    public NodeLink Link { get; set; } = new();
}

public sealed class NodeLink
{
    private string nodeName = "Root";

    /// <summary>
    /// Gets how many times the name was set.
    /// </summary>
    public int Sets { get; private set; }

    public string NodeName
    {
        get => nodeName;
        set
        {
            nodeName = value;
            Sets++;
        }
    }
}
