// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Services;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Engine;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// The Pick button of a component property: the user picks an entity of the asset, then one of its components of the type of the
/// property, in the entity picker.
/// </summary>
public sealed class PickupEntityComponentCommand : PickupSceneObjectCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "PickupComponent";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        if (typeof(EntityComponent).IsAssignableFrom(nodePresenter.Type))
            return true;

        return nodePresenter.Type.IsInterface && nodePresenter.Type.IsImplementedOnAny<EntityComponent>();
    }

    /// <inheritdoc/>
    /// <remarks>Without the type of the property, any component can be picked.</remarks>
    protected override Type? GetComponentType(Type? targetType) => targetType ?? typeof(EntityComponent);

    /// <inheritdoc/>
    protected override object ConvertPickerValue(object newValue, object parameter)
    {
        var picked = (IPickedEntity)newValue;
        return (picked.ComponentIndex >= 0 ? picked.Entity?.AssetSideEntity.Components[picked.ComponentIndex] : null)!;
    }
}
