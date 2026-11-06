// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// The panels the editor can show, each with its owner: the editor itself, or the plugin that registered it and whose
/// panels go when it leaves (<see cref="UnregisterOwner"/>). The View menu and the docking shell follow its changes.
/// </summary>
public sealed class PanelRegistry : INotifyCollectionChanged
{
    private sealed record Entry(PanelDescriptor Descriptor, object? Owner);

    private sealed class Registration(PanelRegistry registry, Entry entry) : IDisposable
    {
        public void Dispose() => registry.Remove(entry);
    }

    private readonly List<Entry> entries = [];

    /// <summary>
    /// The registered panels, in registration order.
    /// </summary>
    public IReadOnlyList<PanelDescriptor> Panels => entries.Select(x => x.Descriptor).ToList();

    /// <summary>
    /// Raised when a panel is registered or unregistered.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Registers a panel.
    /// </summary>
    /// <param name="descriptor">The panel.</param>
    /// <param name="owner">The plugin that registers it, or null for a panel of the editor.</param>
    /// <returns>An object that unregisters the panel when disposed.</returns>
    /// <exception cref="ArgumentException">The identifier is empty, or another panel has it.</exception>
    public IDisposable Register(PanelDescriptor descriptor, object? owner = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (string.IsNullOrWhiteSpace(descriptor.Id))
            throw new ArgumentException("A panel needs an identifier.", nameof(descriptor));
        if (Find(descriptor.Id) is not null)
            throw new ArgumentException($"A panel with the identifier [{descriptor.Id}] is already registered.", nameof(descriptor));

        var entry = new Entry(descriptor, owner);
        entries.Add(entry);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, descriptor, entries.Count - 1));
        return new Registration(this, entry);
    }

    /// <summary>
    /// Unregisters a panel.
    /// </summary>
    /// <returns>True if the panel was registered.</returns>
    public bool Unregister(string panelId)
    {
        var entry = entries.FirstOrDefault(x => x.Descriptor.Id == panelId);
        return entry is not null && Remove(entry);
    }

    /// <summary>
    /// Unregisters the panels of an owner, as when a plugin leaves.
    /// </summary>
    /// <returns>The number of panels unregistered.</returns>
    public int UnregisterOwner(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var owned = entries.Where(x => Equals(x.Owner, owner)).ToList();
        foreach (var entry in owned)
            Remove(entry);
        return owned.Count;
    }

    /// <summary>
    /// The panel with the given identifier, or null.
    /// </summary>
    public PanelDescriptor? Find(string panelId) => entries.FirstOrDefault(x => x.Descriptor.Id == panelId)?.Descriptor;

    /// <summary>
    /// Whether a panel with the given identifier is registered.
    /// </summary>
    public bool Contains(string panelId) => Find(panelId) is not null;

    /// <summary>
    /// The owner of a panel: null for a panel of the editor or a panel that is not registered.
    /// </summary>
    public object? GetOwner(string panelId) => entries.FirstOrDefault(x => x.Descriptor.Id == panelId)?.Owner;

    private bool Remove(Entry entry)
    {
        var index = entries.IndexOf(entry);
        if (index < 0)
            return false;

        entries.RemoveAt(index);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, entry.Descriptor, index));
        return true;
    }
}
