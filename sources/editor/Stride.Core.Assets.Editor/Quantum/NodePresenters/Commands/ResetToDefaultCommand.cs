// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// Gives a property its default value (<see cref="MemberDefaultValues"/>), or, when the property shows another inline and only
/// that one differs from its default, gives that one its default. Run on several objects, it reverts the property of each one
/// that differs; the property grid runs it in one transaction, so it is one undo step.
/// </summary>
/// <remarks>
/// It is not one of the commands of every property grid: an editor that shows the revert of a property registers it, with
/// <see cref="Updaters.DefaultValueNodeUpdater"/>, which says which properties differ from their default.
/// </remarks>
public sealed class ResetToDefaultCommand : SyncNodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "ResetToDefault";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return MemberDefaultValues.CanHaveDefault(nodePresenter);
    }

    /// <inheritdoc/>
    public override bool CanExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object? parameter)
    {
        return nodePresenters.Any(MemberDefaultValues.IsDifferentFromDefault);
    }

    /// <inheritdoc/>
    protected override void ExecuteSync(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        if (MemberDefaultValues.DiffersFromDefault(nodePresenter) == true)
        {
            if (MemberDefaultValues.TryGetDefaultValue(nodePresenter, out var defaultValue))
                nodePresenter.UpdateValue(defaultValue);
        }
        else if (MemberDefaultValues.GetInlinedProperty(nodePresenter) is { } inlined && MemberDefaultValues.DiffersFromDefault(inlined) == true
                 && MemberDefaultValues.TryGetDefaultValue(inlined, out var inlinedDefault))
        {
            inlined.UpdateValue(inlinedDefault);
        }
    }
}
