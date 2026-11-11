// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// Brings a panel forward when a collection receives what the user should see, as the activation behaviors of WPF's panels:
/// the asset view when a folder is selected, the Output when a log receives an error.
/// </summary>
/// <remarks>
/// As in WPF, the first matching addition after the collection was cleared starts it: from then on, every change of the
/// collection brings the panel forward again, until the collection is cleared.
/// The collection must raise its changes on the UI thread, since the panel service is used from there only.
/// </remarks>
public sealed class PanelActivation : IDisposable
{
    private readonly IPanelService panels;
    private readonly string panelId;
    private readonly INotifyCollectionChanged collection;
    private readonly Func<IList, bool> matches;
    private bool matched;

    private PanelActivation(IPanelService panels, string panelId, INotifyCollectionChanged collection, Func<IList, bool> matches)
    {
        this.panels = panels ?? throw new ArgumentNullException(nameof(panels));
        this.panelId = panelId ?? throw new ArgumentNullException(nameof(panelId));
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.matches = matches;
        collection.CollectionChanged += CollectionChanged;
    }

    /// <summary>
    /// Brings the panel forward when a log receives a message of the given level or above (WPF's Output, on an error of the
    /// build, project or live scripting logs).
    /// </summary>
    /// <param name="panels">The panels of the editor.</param>
    /// <param name="panelId">The panel to bring forward.</param>
    /// <param name="messages">The messages of the log.</param>
    /// <param name="minimumLevel">The lowest level that brings the panel forward.</param>
    public static PanelActivation OnLog(IPanelService panels, string panelId, INotifyCollectionChanged messages, LogMessageType minimumLevel = LogMessageType.Error)
    {
        return new PanelActivation(panels, panelId, messages, items => items.OfType<ILogMessage>().Any(x => x.IsAtLeast(minimumLevel)));
    }

    /// <summary>
    /// Brings the panel forward when a folder (or the assets or code folder of a package) is selected in the solution explorer
    /// (WPF's asset view).
    /// </summary>
    /// <param name="panels">The panels of the editor.</param>
    /// <param name="panelId">The panel to bring forward.</param>
    /// <param name="locations">The selected locations.</param>
    public static PanelActivation OnLocationChanged(IPanelService panels, string panelId, INotifyCollectionChanged locations)
    {
        return new PanelActivation(panels, panelId, locations, items => items.OfType<DirectoryBaseViewModel>().Any());
    }

    public void Dispose()
    {
        collection.CollectionChanged -= CollectionChanged;
    }

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
            matched = false;
        if (e.Action == NotifyCollectionChangedAction.Add && !matched && e.NewItems is { } items && matches(items))
            matched = true;
        if (matched)
            panels.Show(panelId);
    }
}
