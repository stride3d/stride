// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Core;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Reflection;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// Opens a curve property (an <c>IComputeCurve&lt;T&gt;</c>, for example the curve of a particle updater) in the curve editor
/// of the session, through its <see cref="ICurveEditorService"/>.
/// </summary>
public sealed class EditCurveCommand : SyncNodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "EditCurve";

    /// <summary>
    /// The separator between the parts of a curve's name.
    /// </summary>
    public const string NameSeparator = " > ";

    /// <summary>
    /// The largest number of parts of a curve's name.
    /// </summary>
    private const int MaximumNameParts = 3;

    /// <summary>
    /// The current session.
    /// </summary>
    private readonly ISessionViewModel session;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditCurveCommand"/> class.
    /// </summary>
    /// <param name="session">The current session.</param>
    public EditCurveCommand(ISessionViewModel session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return CurveEditorViewModel.CanEdit(nodePresenter.Type);
    }

    /// <summary>
    /// The name of the curve of the given property in the curve editor's list: a short path made of the property and its
    /// parents, from the closest item of a collection (an updater, a component) or at most three parts, without the root of the
    /// property grid. For example "Color Animation > Main > Curve".
    /// </summary>
    /// <param name="nodePresenter">The curve property.</param>
    public static string GetCurveName(INodePresenter nodePresenter)
    {
        ArgumentNullException.ThrowIfNull(nodePresenter);

        var parts = new List<string>();
        for (var node = nodePresenter; node is not null && parts.Count < MaximumNameParts; node = node.Parent)
        {
            // The root is the object of the property grid (the entity or the asset): it is not part of the name
            if (node.Parent is null && node != nodePresenter)
                break;

            var part = GetPartName(node);
            if (!string.IsNullOrWhiteSpace(part))
                parts.Insert(0, part);

            // An item of a collection names the object the curve belongs to; the collection above it adds nothing
            if (!node.Index.IsEmpty)
                break;
        }
        return string.Join(NameSeparator, parts);
    }

    private static string GetPartName(INodePresenter node)
    {
        // An item of a collection is shown as "Item 1" by default: the display name of its type ("Color Animation") says more
        if (!node.Index.IsEmpty && node.Value is { } item)
        {
            var typeName = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(item.GetType())?.Name;
            if (!string.IsNullOrWhiteSpace(typeName))
                return typeName;
        }
        return string.IsNullOrWhiteSpace(node.DisplayName) ? node.Name : node.DisplayName;
    }

    /// <inheritdoc/>
    protected override void ExecuteSync(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        var currentValue = nodePresenter.Value;
        if (currentValue is null)
            return;

        session.ServiceProvider.TryGet<ICurveEditorService>()?.Open(currentValue, GetCurveName(nodePresenter));
    }
}
