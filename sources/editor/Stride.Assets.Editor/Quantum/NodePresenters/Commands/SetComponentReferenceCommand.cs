// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Engine;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

internal sealed class SetComponentReferenceCommand : ChangeValueCommandBase
{
    public record struct Parameter(EntityViewModel Entity, int Index);

    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "SetComponentReference";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return typeof(EntityComponent).IsAssignableFrom(nodePresenter.Type) || nodePresenter.Type.IsInterface && nodePresenter.Type.IsImplementedOnAny<EntityComponent>();
    }

    /// <inheritdoc/>
    protected override object? ChangeValue(object currentValue, object? parameter, object? preExecuteResult)
    {
        return parameter is Parameter param ? param.Entity?.AssetSideEntity.Components[param.Index] : null;
    }
}
