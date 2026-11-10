// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Stride.Assets.Editor.ViewModels.GraphicsCompositorEditor;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Collections;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The graph of the graphics compositor editor for the <see cref="NodeGraphControl"/>, the Avalonia counterpart of the WPF
/// <c>GraphicsCompositorGraph</c>: the nodes are the blocks of the editor, the links those of their output slots, gathered in
/// <see cref="Links"/>, and the commands link and unlink through the slots, which change the asset in one undo entry each.
/// </summary>
public sealed class GraphicsCompositorGraph : INodeGraphLinkResolver, IDisposable
{
    private readonly IObservableCollection<IGraphicsCompositorBlockViewModel> blocks;
    private readonly Dictionary<IGraphicsCompositorBlockViewModel, List<IGraphicsCompositorSlotViewModel>> observedSlots = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, IGraphicsCompositorSlotViewModel> slotOfLinks = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsCompositorGraph"/> class.
    /// </summary>
    /// <param name="blocks">The blocks of the editor, <see cref="ViewModels.GraphicsCompositorEditorViewModel.Blocks"/>.</param>
    public GraphicsCompositorGraph(IObservableCollection<IGraphicsCompositorBlockViewModel> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        this.blocks = blocks;
        blocks.CollectionChanged += OnBlocksChanged;
        foreach (var block in blocks)
            AddBlock(block);

        LinkCommand = new GraphCommand(CanLink, Link);
        UnlinkCommand = new GraphCommand(x => x is IGraphicsCompositorLinkViewModel, Unlink);
    }

    /// <summary>
    /// The links of the output slots of the blocks.
    /// </summary>
    public ObservableCollection<IGraphicsCompositorLinkViewModel> Links { get; } = [];

    /// <summary>
    /// Links an output slot to the input slot of a shared renderer, with a <see cref="NodeGraphLinkRequest"/>: the output slot
    /// references the renderer instead of the one it referenced (<see cref="IGraphicsCompositorSlotViewModel.LinkTo"/>).
    /// </summary>
    public ICommand LinkCommand { get; }

    /// <summary>
    /// Removes a link: its output slot references no renderer any more (<see cref="IGraphicsCompositorSlotViewModel.ClearLink"/>).
    /// </summary>
    public ICommand UnlinkCommand { get; }

    /// <inheritdoc/>
    public object? GetSourceSlot(object link) => (link as IGraphicsCompositorLinkViewModel)?.SourceSlot;

    /// <inheritdoc/>
    public object? GetTargetSlot(object link) => (link as IGraphicsCompositorLinkViewModel)?.TargetSlot;

    /// <inheritdoc/>
    public void Dispose()
    {
        blocks.CollectionChanged -= OnBlocksChanged;
        foreach (var block in observedSlots.Keys.ToList())
            RemoveBlock(block);
    }

    private static bool CanLink(object? parameter)
    {
        // An output slot of a block can reference a renderer that its member accepts; an input slot links nothing itself
        return parameter is NodeGraphLinkRequest { SourceSlot: GraphicsCompositorSlotViewModel source, TargetSlot: GraphicsCompositorSlotViewModel target }
            && source.CanLinkTo(target);
    }

    private static void Link(object? parameter)
    {
        // A slot references one renderer: linking it again replaces the link the user moved, in the same undo entry
        if (parameter is NodeGraphLinkRequest { SourceSlot: GraphicsCompositorSlotViewModel source, TargetSlot: GraphicsCompositorSlotViewModel target })
            source.LinkTo(target);
    }

    private static void Unlink(object? parameter)
    {
        if (parameter is IGraphicsCompositorLinkViewModel link)
            link.SourceSlot.ClearLink();
    }

    private void AddBlock(IGraphicsCompositorBlockViewModel block)
    {
        if (observedSlots.ContainsKey(block))
            return;

        observedSlots.Add(block, []);
        block.OutputSlots.CollectionChanged += OnOutputSlotsChanged;
        foreach (var slot in block.OutputSlots)
            AddSlot(block, slot);
    }

    private void RemoveBlock(IGraphicsCompositorBlockViewModel block)
    {
        if (!observedSlots.Remove(block, out var slots))
            return;

        block.OutputSlots.CollectionChanged -= OnOutputSlotsChanged;
        foreach (var slot in slots)
            RemoveSlotLinks(slot);
    }

    private void AddSlot(IGraphicsCompositorBlockViewModel block, IGraphicsCompositorSlotViewModel slot)
    {
        var slots = observedSlots[block];
        if (slots.Contains(slot))
            return;

        slots.Add(slot);
        slot.Links.CollectionChanged += OnSlotLinksChanged;
        foreach (var link in slot.Links)
            AddLink(slot, link);
    }

    private void RemoveSlotLinks(IGraphicsCompositorSlotViewModel slot)
    {
        slot.Links.CollectionChanged -= OnSlotLinksChanged;
        foreach (var link in slotOfLinks.Where(x => ReferenceEquals(x.Value, slot)).Select(x => x.Key).ToList())
            RemoveLink(link);
    }

    private void AddLink(IGraphicsCompositorSlotViewModel slot, IGraphicsCompositorLinkViewModel link)
    {
        if (!slotOfLinks.TryAdd(link, slot))
            return;

        Links.Add(link);
    }

    private void RemoveLink(object link)
    {
        if (slotOfLinks.Remove(link))
            Links.Remove((IGraphicsCompositorLinkViewModel)link);
    }

    private IGraphicsCompositorBlockViewModel? FindBlock(object? outputSlots)
        => observedSlots.Keys.FirstOrDefault(x => ReferenceEquals(x.OutputSlots, outputSlots));

    private IGraphicsCompositorSlotViewModel? FindSlot(object? links)
        => observedSlots.Values.SelectMany(x => x).FirstOrDefault(x => ReferenceEquals(x.Links, links));

    private void OnBlocksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var block in observedSlots.Keys.ToList())
                RemoveBlock(block);
            foreach (var block in blocks)
                AddBlock(block);
            return;
        }

        foreach (IGraphicsCompositorBlockViewModel block in e.OldItems ?? Array.Empty<object>())
            RemoveBlock(block);
        foreach (IGraphicsCompositorBlockViewModel block in e.NewItems ?? Array.Empty<object>())
            AddBlock(block);
    }

    private void OnOutputSlotsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (FindBlock(sender) is not { } block)
            return;

        var slots = observedSlots[block];
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var slot in slots.ToList())
                RemoveSlotLinks(slot);
            slots.Clear();
            foreach (var slot in block.OutputSlots)
                AddSlot(block, slot);
            return;
        }

        foreach (IGraphicsCompositorSlotViewModel slot in e.OldItems ?? Array.Empty<object>())
        {
            RemoveSlotLinks(slot);
            slots.Remove(slot);
        }
        foreach (IGraphicsCompositorSlotViewModel slot in e.NewItems ?? Array.Empty<object>())
            AddSlot(block, slot);
    }

    private void OnSlotLinksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (FindSlot(sender) is not { } slot)
            return;

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var link in slotOfLinks.Where(x => ReferenceEquals(x.Value, slot)).Select(x => x.Key).ToList())
                RemoveLink(link);
            foreach (var link in slot.Links)
                AddLink(slot, link);
            return;
        }

        foreach (var link in e.OldItems ?? Array.Empty<object>())
            RemoveLink(link);
        foreach (IGraphicsCompositorLinkViewModel link in e.NewItems ?? Array.Empty<object>())
            AddLink(slot, link);
    }

    private sealed class GraphCommand(Func<object?, bool> canExecute, Action<object?> execute) : ICommand
    {
        /// <inheritdoc/>
        /// <remarks>The graph asks again at each move of a dragged link.</remarks>
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => canExecute(parameter);

        public void Execute(object? parameter) => execute(parameter);
    }
}
