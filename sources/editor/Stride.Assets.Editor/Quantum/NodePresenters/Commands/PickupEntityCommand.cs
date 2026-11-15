// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Services;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Engine;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// The Pick button of an entity property: the user picks an entity of the asset in the entity picker.
/// </summary>
public sealed class PickupEntityCommand : PickupSceneObjectCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "PickupEntity";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return typeof(Entity).IsAssignableFrom(nodePresenter.Type);
    }

    /// <inheritdoc/>
    protected override Type? GetComponentType(Type? targetType) => null;

    /// <inheritdoc/>
    protected override object ConvertPickerValue(object newValue, object parameter)
    {
        return ((IPickedEntity)newValue).Entity?.AssetSideEntity!;
    }
}
