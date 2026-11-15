// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Components.Transactions;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// A built-in panel whose view shows a part of the session.
/// </summary>
public abstract class SessionPanelViewModel : PanelViewModel
{
    private protected SessionPanelViewModel(PanelContext context)
        : base(context)
    {
        Session = context.EditorSession;
    }

    /// <summary>
    /// The session.
    /// </summary>
    public SessionViewModel Session { get; }
}

/// <summary>
/// The Solution explorer: the packages of the session, their folders and their dependencies.
/// </summary>
public sealed class SolutionExplorerPanelViewModel(PanelContext context) : SessionPanelViewModel(context);

/// <summary>
/// The Asset view: the assets of the folders selected in the solution explorer.
/// </summary>
public sealed class AssetViewPanelViewModel(PanelContext context) : SessionPanelViewModel(context);

/// <summary>
/// The Property grid: the properties of the selected assets or of the selection of the active editor.
/// </summary>
public sealed class PropertyGridPanelViewModel(PanelContext context) : SessionPanelViewModel(context);

/// <summary>
/// The References: the assets the selected assets use, or the assets that use them.
/// </summary>
public sealed class ReferencesPanelViewModel : SessionPanelViewModel
{
    public ReferencesPanelViewModel(PanelContext context)
        : base(context)
    {
    }

    /// <summary>
    /// The references of the selected assets.
    /// </summary>
    public ReferencesViewModel References => Session.References;
}

/// <summary>
/// The Edit history: the transactions of the session, with its save point.
/// </summary>
public sealed class ActionHistoryPanelViewModel : PanelViewModel
{
    public ActionHistoryPanelViewModel(PanelContext context)
        : base(context)
    {
        ActionHistory = context.EditorSession.ActionHistory;
    }

    /// <summary>
    /// The history of the session.
    /// </summary>
    public ActionHistoryViewModel ActionHistory { get; }
}

/// <summary>
/// The Asset errors: the warnings and errors of the assets, with their count in the title, as in WPF.
/// </summary>
public sealed class AssetLogPanelViewModel : PanelViewModel
{
    public AssetLogPanelViewModel(PanelContext context)
        : base(context)
    {
        AssetLog = context.EditorSession.AssetLog;
        AssetLog.PropertyChanged += AssetLogPropertyChanged;
        UpdateTitle();
    }

    /// <summary>
    /// The log of the assets.
    /// </summary>
    public AssetLogViewModel AssetLog { get; }

    /// <inheritdoc />
    public override void Destroy()
    {
        AssetLog.PropertyChanged -= AssetLogPropertyChanged;
        base.Destroy();
    }

    private void AssetLogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssetLogViewModel.ErrorCount))
            UpdateTitle();
    }

    private void UpdateTitle()
    {
        var count = AssetLog.ErrorCount;
        Title = string.Format(Tr._pn("View", "Asset error ({0})", "Asset errors ({0})", count), count);
    }
}

/// <summary>
/// The Output: the log of the last build and of the games started from the editor. Its title has a "*" while it has
/// messages that were not seen, and a build brings it forward, as in WPF.
/// </summary>
public sealed class OutputPanelViewModel : PanelViewModel
{
    public OutputPanelViewModel(PanelContext context, BuildAndRunViewModel build)
        : base(context)
    {
        Build = build ?? throw new ArgumentNullException(nameof(build));
        Build.PropertyChanged += BuildPropertyChanged;
        Title = Build.OutputTitle;
    }

    /// <summary>
    /// The builds of the editor, with their log.
    /// </summary>
    public BuildAndRunViewModel Build { get; }

    /// <inheritdoc />
    public override void Destroy()
    {
        Build.PropertyChanged -= BuildPropertyChanged;
        base.Destroy();
    }

    /// <inheritdoc />
    protected override void OnIsShownChanged()
    {
        // The messages are seen while the panel is shown
        Build.IsOutputVisible = IsShown;
    }

    private void BuildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BuildAndRunViewModel.OutputTitle))
            Title = Build.OutputTitle;
    }
}
