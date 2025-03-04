// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum.Visitors;

/// <summary>
/// A visitor that clear object references to a specific identifiable object.
/// </summary>
public class ClearObjectReferenceVisitor : IdentifiableObjectVisitorBase
{
    private readonly HashSet<Guid> targetIds;
    private readonly Func<IGraphNode, NodeIndex, bool>? shouldClearReference;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClearObjectReferenceVisitor"/> class.
    /// </summary>
    /// <param name="propertyGraphDefinition">The <see cref="AssetPropertyGraphDefinition"/> used to analyze object references.</param>
    /// <param name="targetIds">The identifiers of the objects for which to clear references.</param>
    /// <param name="shouldClearReference">A method allowing to select which object reference to clear. If null, all object references to the given id will be cleared.</param>
    public ClearObjectReferenceVisitor(AssetPropertyGraphDefinition propertyGraphDefinition, IEnumerable<Guid> targetIds, Func<IGraphNode, NodeIndex, bool>? shouldClearReference = null)
        : base(propertyGraphDefinition)
    {
        ArgumentNullException.ThrowIfNull(propertyGraphDefinition);
        ArgumentNullException.ThrowIfNull(targetIds);
        this.targetIds = new HashSet<Guid>(targetIds);
        this.shouldClearReference = shouldClearReference;
    }

    /// <inheritdoc/>
    protected override void ProcessIdentifiableMembers(IIdentifiable identifiable, IMemberNode member)
    {
        if (!targetIds.Contains(identifiable.Id))
            return;

        if (PropertyGraphDefinition.IsMemberTargetObjectReference(member, identifiable))
        {
            if (shouldClearReference?.Invoke(member, NodeIndex.Empty) ?? true)
            {
                member.Update(null);
            }
        }
    }

    /// <inheritdoc/>
    protected override void ProcessIdentifiableItems(IIdentifiable identifiable, IObjectNode collection, NodeIndex index)
    {
        if (!targetIds.Contains(identifiable.Id))
            return;

        if (PropertyGraphDefinition.IsTargetItemObjectReference(collection, index, identifiable))
        {
            if (shouldClearReference?.Invoke(collection, index) ?? true)
            {
                collection.Update(null, index);
            }
        }
    }
}
