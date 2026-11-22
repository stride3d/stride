// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;

/// <summary>
/// Says on each property that has a default value whether its value, or the value of the property it shows inline, differs
/// from its default (<see cref="DefaultValueData.DiffersFromDefaultKey"/>). It runs again on a property when its value changes.
/// </summary>
/// <remarks>
/// It is not one of the updaters of every property grid: an editor that shows the revert of a property to its default
/// registers it, with <see cref="Commands.ResetToDefaultCommand"/>.
/// </remarks>
public sealed class DefaultValueNodeUpdater : NodePresenterUpdaterBase
{
    // The properties whose changes of the value only this updater follows, each once
    private readonly ConditionalWeakTable<INodePresenter, object?> followedNodes = new();

    /// <inheritdoc/>
    public override void UpdateNode(INodePresenter node)
    {
        var hasInlinedProperty = MemberDefaultValues.GetInlinedProperty(node) is not null;
        if (!Update(node) && !hasInlinedProperty)
            return;

        // A change of the value only keeps the attached properties and doesn't run the updaters again (see
        // INodePresenter.RefreshOnValueChange): this updater follows those changes itself, before the view model of the node
        if (followedNodes.TryAdd(node, null))
            node.ValueChanged += ValueChanged;
    }

    private static void ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (!e.IsValueOnly || sender is not INodePresenter node)
            return;

        // The property shown inline first, since this one differs when that one does
        if (MemberDefaultValues.GetInlinedProperty(node) is { } inlined)
            Update(inlined);
        Update(node);
    }

    // Returns whether the property has a default value
    private static bool Update(INodePresenter node)
    {
        var differs = MemberDefaultValues.DiffersFromDefault(node);

        // The children are updated first: a property that shows another inline (a color provider shows its color) differs when
        // that one does
        if (MemberDefaultValues.GetInlinedProperty(node) is { } inlined && MemberDefaultValues.IsDifferentFromDefault(inlined))
            differs = true;

        if (differs is not { } value)
            return false;

        node.AttachedProperties.Set(DefaultValueData.DiffersFromDefaultKey, value);
        return true;
    }
}
