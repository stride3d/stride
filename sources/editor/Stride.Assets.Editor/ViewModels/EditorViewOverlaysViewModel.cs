// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Core.Presentation.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The overlays popup of the view of a scene editor: one section per <see cref="IEditorGameOverlayService"/> of the game
/// (the navigation meshes of the Physics editor package, for instance), as the overlays popup of the WPF scene editor.
/// </summary>
/// <remarks>
/// The agnostic copy of the WPF <c>EditorOverlaysViewModel</c>. The visibilities are not saved with the scene yet: the
/// overlays start with the visibility their service gives them.
/// </remarks>
public sealed class EditorViewOverlaysViewModel : DispatcherViewModel
{
    private readonly List<(IEditorGameOverlayService Service, EventHandler Handler)> subscriptions = [];

    public EditorViewOverlaysViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<IEditorGameOverlayService> services)
        : base(serviceProvider)
    {
        ToggleAllCommand = new AnonymousCommand<bool>(ServiceProvider, value => Sections.SelectMany(x => x.Overlays).ForEach(x => x.IsVisible = value));
        foreach (var service in services)
        {
            var section = new EditorViewOverlaySectionViewModel(ServiceProvider, service);
            Sections.Add(section);
            EventHandler handler = (_, _) => Dispatcher.InvokeAsync(() => RebuildSection(section));
            service.OverlaysChanged += handler;
            subscriptions.Add((service, handler));
            RebuildSection(section);
        }
    }

    /// <summary>
    /// Gets the sections of the popup, one per overlay service, in the order of the services.
    /// </summary>
    public ObservableList<EditorViewOverlaySectionViewModel> Sections { get; } = [];

    /// <summary>
    /// Gets whether the game has any overlay service: the toolbar shows the button only then.
    /// </summary>
    public bool HasSections => Sections.Count > 0;

    /// <summary>
    /// Gets the command that shows (<c>true</c>) or hides (<c>false</c>) every overlay.
    /// </summary>
    public ICommandBase ToggleAllCommand { get; }

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EditorViewOverlaysViewModel));

        foreach (var (service, handler) in subscriptions)
            service.OverlaysChanged -= handler;
        subscriptions.Clear();
        base.Destroy();
    }

    /// <summary>
    /// Rebuilds the toggles of a section from its service; known overlays keep their visibility, new ones take their default.
    /// </summary>
    private void RebuildSection(EditorViewOverlaySectionViewModel section)
    {
        if (IsDestroyed)
            return;

        var previousVisibility = section.Overlays.ToDictionary(x => x.Key, x => x.IsVisible);
        section.Overlays.Clear();
        foreach (var overlay in section.Service.Overlays)
        {
            var isVisible = previousVisibility.TryGetValue(overlay.Key, out var previous) ? previous : overlay.VisibleByDefault;
            section.Overlays.Add(new EditorViewOverlayViewModel(ServiceProvider, section.Service, overlay, isVisible));
        }
    }
}

/// <summary>
/// The toggles of one <see cref="IEditorGameOverlayService"/>, under its heading.
/// </summary>
public sealed class EditorViewOverlaySectionViewModel : DispatcherViewModel
{
    public EditorViewOverlaySectionViewModel(IViewModelServiceProvider serviceProvider, IEditorGameOverlayService service)
        : base(serviceProvider)
    {
        Service = service;
    }

    /// <summary>
    /// Gets the service drawing the overlays of this section.
    /// </summary>
    public IEditorGameOverlayService Service { get; }

    /// <summary>
    /// Gets the heading of the section.
    /// </summary>
    public string DisplayName => Service.DisplayName;

    /// <summary>
    /// Gets the toggles of the section.
    /// </summary>
    public ObservableList<EditorViewOverlayViewModel> Overlays { get; } = [];
}

/// <summary>
/// One overlay toggle.
/// </summary>
public sealed class EditorViewOverlayViewModel : DispatcherViewModel
{
    private readonly IEditorGameOverlayService service;
    private bool isVisible;

    public EditorViewOverlayViewModel(IViewModelServiceProvider serviceProvider, IEditorGameOverlayService service, EditorOverlay overlay, bool isVisible)
        : base(serviceProvider)
    {
        this.service = service;
        Key = overlay.Key;
        DisplayName = overlay.DisplayName;
        Color = overlay.Color;
        this.isVisible = isVisible;
        service.SetOverlayVisible(Key, isVisible);
    }

    /// <summary>
    /// Gets the key of the overlay in its service.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the name of the overlay.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the color the overlay is drawn with, if it has one.
    /// </summary>
    public Color4? Color { get; }

    /// <summary>
    /// Gets whether the overlay has a color to show next to its name.
    /// </summary>
    public bool HasColor => Color.HasValue;

    /// <summary>
    /// Gets or sets whether the overlay is drawn.
    /// </summary>
    public bool IsVisible
    {
        get => isVisible;
        set => SetValue(ref isVisible, value, () => service.SetOverlayVisible(Key, value));
    }
}
