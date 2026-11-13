// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using Stride.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels.CurveEditor;

/// <summary>
/// The <see cref="ICurveEditorService"/> of an editor whose curve editor is a panel (<see cref="CurveEditorPanelViewModel"/>):
/// opening a curve shows the panel and adds the curve to it, or selects it if it is already there.
/// </summary>
public sealed class CurveEditorPanelService : ICurveEditorService
{
    private readonly ISessionViewModel session;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurveEditorPanelService"/> class.
    /// </summary>
    /// <param name="session">The session; its services give the <see cref="IPanelService"/> of the main window.</param>
    public CurveEditorPanelService(ISessionViewModel session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// The curve editor of the panel, once the panel exists (the main window creates it with the session's panels).
    /// </summary>
    public CurveEditorViewModel? Editor => (Panels?.GetPanel(CurveEditorPanelViewModel.PanelId) as CurveEditorPanelViewModel)?.Editor;

    private IPanelService? Panels => session.ServiceProvider.TryGet<IPanelService>();

    /// <inheritdoc/>
    public void Open(object curve, string name)
    {
        ArgumentNullException.ThrowIfNull(curve);
        if (Panels is not { } panels)
            return;

        panels.Show(CurveEditorPanelViewModel.PanelId);
        if (Editor is not { } editor)
            return;

        // As in WPF: the curve is added (or selected), and the view fits it
        editor.AddCurve(curve, name);
        editor.Focus();
    }

    /// <inheritdoc/>
    public void Close()
    {
        Panels?.Hide(CurveEditorPanelViewModel.PanelId);
    }
}
