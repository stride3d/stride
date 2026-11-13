// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// A registry of contributions that a layer of the editor declares with the plugin host (<see cref="EditorPluginHost.DeclareRegistry{T}"/>),
/// so that the plugins register into it with <see cref="IEditorPluginContext.GetRegistry{T}"/> although the context does not name it.
/// Each contribution has an owner, the plugin that registered it, and goes when that plugin leaves.
/// </summary>
/// <remarks>
/// Changes happen on the editor's thread. <see cref="Items"/> can be read from any thread: it is replaced on each change,
/// never changed.
/// </remarks>
/// <typeparam name="T">The type of the contributions.</typeparam>
[Experimental("STREXP001")]
public sealed class ContributionRegistry<T> : INotifyCollectionChanged
    where T : class
{
    private sealed record Entry(T Item, object? Owner);

    private sealed class Registration(ContributionRegistry<T> registry, Entry entry) : IDisposable
    {
        public void Dispose() => registry.Remove(entry);
    }

    private readonly Action<T>? validate;
    private volatile Entry[] entries = [];

    /// <param name="validate">Checks a contribution before it is registered, and throws to refuse it; or null.</param>
    public ContributionRegistry(Action<T>? validate = null)
    {
        this.validate = validate;
    }

    /// <summary>
    /// The registered contributions, in registration order.
    /// </summary>
    public IReadOnlyList<T> Items => entries.Select(x => x.Item).ToList();

    /// <summary>
    /// Raised when a contribution is registered or unregistered.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Registers a contribution.
    /// </summary>
    /// <param name="item">The contribution.</param>
    /// <param name="owner">The plugin that registers it, or null for a contribution of the editor.</param>
    /// <returns>An object that unregisters the contribution when disposed.</returns>
    /// <exception cref="ArgumentException">The contribution is already registered, or the registry refuses it.</exception>
    public IDisposable Register(T item, object? owner = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (entries.Any(x => ReferenceEquals(x.Item, item)))
            throw new ArgumentException($"The contribution [{item}] is already registered.", nameof(item));
        validate?.Invoke(item);

        var entry = new Entry(item, owner);
        entries = [.. entries, entry];
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, entries.Length - 1));
        return new Registration(this, entry);
    }

    /// <summary>
    /// Unregisters a contribution.
    /// </summary>
    /// <returns>True if the contribution was registered.</returns>
    public bool Unregister(T item)
    {
        var entry = entries.FirstOrDefault(x => ReferenceEquals(x.Item, item));
        return entry is not null && Remove(entry);
    }

    /// <summary>
    /// Unregisters the contributions of an owner, as when a plugin leaves.
    /// </summary>
    /// <returns>The number of contributions unregistered.</returns>
    public int UnregisterOwner(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var owned = entries.Where(x => Equals(x.Owner, owner)).ToList();
        foreach (var entry in owned)
            Remove(entry);
        return owned.Count;
    }

    /// <summary>
    /// The owner of a contribution: null for a contribution of the editor or one that is not registered.
    /// </summary>
    public object? GetOwner(T item) => entries.FirstOrDefault(x => ReferenceEquals(x.Item, item))?.Owner;

    private bool Remove(Entry entry)
    {
        var current = entries;
        var index = Array.IndexOf(current, entry);
        if (index < 0)
            return false;

        entries = [.. current[..index], .. current[(index + 1)..]];
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, entry.Item, index));
        return true;
    }
}
