// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia.Controls;
using Dock.Model.Inpc.Controls;
using Stride.Core.Assets.Editor.Panels;

namespace Stride.GameStudio.Avalonia.Docking;

/// <summary>
/// A panel in Dock's model: its view is the view of its <see cref="ViewModel"/> (see <see cref="DockViewLocator"/>).
/// </summary>
public sealed class PanelDockable : Tool, IDisposable, IDockableView
{
    private readonly Func<PanelDockable, bool> close;
    private readonly Func<IPanelViewModel, Control>? createPluginView;

    internal PanelDockable(IPanelViewModel viewModel, bool allowsMultipleInstances, Func<PanelDockable, bool> close, Func<IPanelViewModel, Control>? createPluginView = null)
    {
        this.createPluginView = createPluginView;
        ViewModel = viewModel;
        Ref = new PanelRef(viewModel.PanelId, viewModel.InstanceId);
        AllowsMultipleInstances = allowsMultipleInstances;
        this.close = close;
        Id = Ref.ToString();
        Title = viewModel.Title;
        CanClose = true;
        CanFloat = true;
        CanPin = true;
        // A panel is not an editor: it stays out of the editors' area
        CanDockAsDocument = false;
        if (viewModel is INotifyPropertyChanged notify)
            notify.PropertyChanged += ViewModelPropertyChanged;
    }

    /// <summary>
    /// The view model of the panel.
    /// </summary>
    public IPanelViewModel ViewModel { get; }

    /// <summary>
    /// The panel in the saved layouts.
    /// </summary>
    public PanelRef Ref { get; }

    /// <summary>
    /// Whether this is an instance of a panel that can be opened several times: it closes, where a single panel hides.
    /// </summary>
    public bool AllowsMultipleInstances { get; }

    /// <summary>
    /// The close button of the tab: the panel service hides the panel, or closes the instance once it agrees, and Dock does
    /// not remove it itself.
    /// </summary>
    public override bool OnClose() => close(this);

    /// <inheritdoc />
    Control? IDockableView.View { get; set; }

    /// <summary>
    /// Creates the view of a panel that the shell does not know: the view its plugin registered, or a placeholder.
    /// </summary>
    internal Control? CreatePluginView() => createPluginView?.Invoke(ViewModel);

    public void Dispose()
    {
        ((IDockableView)this).View = null;
        if (ViewModel is INotifyPropertyChanged notify)
            notify.PropertyChanged -= ViewModelPropertyChanged;
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPanelViewModel.Title))
            Title = ViewModel.Title;
    }
}
