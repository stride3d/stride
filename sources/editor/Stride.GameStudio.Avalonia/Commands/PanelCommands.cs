// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;

namespace Stride.GameStudio.Avalonia.Commands;

/// <summary>
/// A command per panel that is opened once, which shows or hides it: the toggles of the View menu, in the command registry
/// (so in the command palette, and with a shortcut if the user gives one). It follows the panels that the editor and the plugins
/// register; a panel that can be opened several times has none (its View menu entries open or bring forward an instance).
/// </summary>
/// <remarks>
/// The commands are the editor's own (registered with no owner): they come and go with their panel.
/// </remarks>
internal sealed class PanelCommands : IDisposable
{
    /// <summary>
    /// The start of the identifier of a panel's command; the panel's identifier follows.
    /// </summary>
    public const string IdPrefix = "Stride.View.Panel.";

    private readonly EditorPluginHost host;
    private readonly IPanelService panels;
    private readonly Dictionary<string, IDisposable> registrations = [];
    private bool disposed;

    public PanelCommands(EditorPluginHost host, IPanelService panels)
    {
        this.host = host;
        this.panels = panels;
        panels.Registry.CollectionChanged += OnRegistryChanged;
        Update();
    }

    /// <summary>
    /// The identifier of the command that shows or hides a panel.
    /// </summary>
    public static string IdOf(string panelId) => IdPrefix + panelId;

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        panels.Registry.CollectionChanged -= OnRegistryChanged;
        foreach (var registration in registrations.Values)
            registration.Dispose();
        registrations.Clear();
    }

    private void OnRegistryChanged(object? sender, NotifyCollectionChangedEventArgs e) => Update();

    private void Update()
    {
        if (disposed)
            return;
        var current = panels.Registry.Panels.Where(x => !x.AllowsMultipleInstances).ToDictionary(x => x.Id);
        foreach (var panelId in registrations.Keys.Where(x => !current.ContainsKey(x)).ToList())
        {
            registrations[panelId].Dispose();
            registrations.Remove(panelId);
        }
        foreach (var (panelId, descriptor) in current)
        {
            if (registrations.ContainsKey(panelId))
                continue;
            var title = descriptor.Title;
            registrations[panelId] = host.Commands.Register(new CommandDescriptor
            {
                Id = IdOf(panelId),
                Category = CommandCategories.View,
                Title = title,
                Keywords = ["panel", "show", "hide", "window"],
                IsChecked = _ => panels.IsVisible(panelId),
                CanExecute = _ => panels.Registry.Contains(panelId),
                ExecuteAsync = _ =>
                {
                    if (panels.IsVisible(panelId))
                        panels.Hide(panelId);
                    else
                        panels.Show(panelId);
                    return Task.CompletedTask;
                },
            });
        }
    }
}
