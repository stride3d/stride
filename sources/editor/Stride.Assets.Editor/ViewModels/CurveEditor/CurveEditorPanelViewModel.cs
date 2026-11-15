// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.ViewModels.CurveEditor;

/// <summary>
/// The curve editor as a panel of the main window: one per session, with the asset view (<see cref="PanelPlacement.Document"/>),
/// listed in the View menu, hidden whenever a session opens since it starts empty. The "Edit curve" button of a curve property
/// shows it through <see cref="CurveEditorPanelService"/>.
/// </summary>
public sealed class CurveEditorPanelViewModel : PanelViewModel
{
    /// <summary>
    /// The identifier of the curve editor panel.
    /// </summary>
    public const string PanelId = "Stride.CurveEditor";

    /// <summary>
    /// Initializes a new instance of the <see cref="CurveEditorPanelViewModel"/> class.
    /// </summary>
    /// <param name="context">The context of the panel.</param>
    public CurveEditorPanelViewModel(PanelContext context)
        : base(context)
    {
        Editor = new CurveEditorViewModel(context.ServiceProvider, context.EditorSession);
    }

    /// <summary>
    /// The curve editor shown in this panel.
    /// </summary>
    public CurveEditorViewModel Editor { get; }

    /// <summary>
    /// The description of the curve editor panel.
    /// </summary>
    public static PanelDescriptor CreateDescriptor()
    {
        return new PanelDescriptor
        {
            Id = PanelId,
            Title = () => Tr._p("Panel", "Curve editor"),
            // After the property grid and the edit history
            MenuGroup = BuiltInPanels.EditMenuGroup,
            Order = 320,
            DefaultPlacement = PanelPlacement.Document,
            VisibleByDefault = false,
            HiddenWhenSessionOpens = true,
            CreateViewModel = context => new CurveEditorPanelViewModel(context),
        };
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        Editor.Destroy();
        base.Destroy();
    }
}
