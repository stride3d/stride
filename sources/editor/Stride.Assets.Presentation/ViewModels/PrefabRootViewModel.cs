// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Stride.Core;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// The root of the hierarchy of the prefab editor.
/// </summary>
[DebuggerDisplay("PrefabRoot = {" + nameof(Name) + "}")]
public sealed class PrefabRootViewModel : EntityHierarchyRootViewModel
{
    /// <param name="asset">The edited prefab.</param>
    /// <param name="editor">The editor of the prefab, or <c>null</c> when the hierarchy has no editor.</param>
    public PrefabRootViewModel(PrefabViewModel asset, IEntityHierarchyEditorViewModel? editor)
        : base(asset, editor)
    {
    }

    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, Guid.Empty);

    /// <inheritdoc/>
    public override string? Name { get => "Prefab root"; set => throw new NotSupportedException($"Cannot change the name of a {nameof(PrefabRootViewModel)} object."); }

    /// <inheritdoc />
    protected override async Task OnLoadingRequested(bool load, bool recursive)
    {
        if (load)
        {
            // Loading/unloading a prefab root always applies to all its content
            SetEntityIsLoadedRecursively(true);
            if (Editor is { } editor)
                await editor.LoadRootsAsync([this]);
        }
        else
        {
            // Loading/unloading a prefab root always applies to all its content
            SetEntityIsLoadedRecursively(false);
            if (Editor is { } editor)
                await editor.UnloadRootsAsync([this]);
        }
        IsLoaded = load;
    }

    /// <inheritdoc />
    protected override Task OnLockingRequested(bool @lock, bool recursive)
    {
        IsLocked = @lock;
        // Locking/unlocking a prefab always applies to all its content
        SetEntityIsLockedRecursively(@lock);
        return Task.CompletedTask;
    }
}
