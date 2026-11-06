// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Views;
using Stride.Core.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// What a plugin adds to the editor when it joins a session, given to <see cref="AssetsPlugin.Initialize(IEditorPluginContext)"/>.
/// The editor records every registration under the plugin that made it and removes it when the plugin's assembly is unloaded.
/// </summary>
public interface IEditorPluginContext
{
    /// <summary>
    /// The plugin being initialized.
    /// </summary>
    AssetsPlugin Plugin { get; }

    /// <summary>
    /// The session the plugin joins.
    /// </summary>
    ISessionViewModel Session { get; }

    /// <summary>
    /// The logger of the session.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// The view model type of each asset type (an <see cref="AssetViewModel"/> for an <see cref="Asset"/>).
    /// </summary>
    IPluginMap<Type, Type> AssetViewModelTypes { get; }

    /// <summary>
    /// The editor view model type of each asset view model type (an <see cref="IAssetEditorViewModel"/>).
    /// </summary>
    IPluginMap<Type, Type> AssetEditorViewModelTypes { get; }

    /// <summary>
    /// The editor view type of each editor view model type, in the editor's toolkit.
    /// </summary>
    IPluginMap<Type, Type> AssetEditorViewTypes { get; }

    /// <summary>
    /// The view model type of each asset preview type.
    /// </summary>
    IPluginMap<Type, Type> AssetPreviewViewModelTypes { get; }

    /// <summary>
    /// The view type of each asset preview type, in the editor's toolkit.
    /// </summary>
    IPluginMap<Type, Type> AssetPreviewViewTypes { get; }

    /// <summary>
    /// The types that the property grid shows as a single value instead of expanding their members.
    /// </summary>
    IPluginRegistry<Type> PrimitiveTypes { get; }

    /// <summary>
    /// The image of an enum value, in the editor's toolkit; the editor shows it in place of the value.
    /// </summary>
    IPluginMap<object, object> EnumImages { get; }

    /// <summary>
    /// The image of a type (an entity component, for instance), in the editor's toolkit.
    /// </summary>
    IPluginMap<Type, object> TypeImages { get; }

    /// <summary>
    /// The template providers of the property grid and of the editors.
    /// </summary>
    IPluginRegistry<ITemplateProvider> TemplateProviders { get; }

    /// <summary>
    /// The processors that turn copied objects into text.
    /// </summary>
    IPluginRegistry<ICopyProcessor> CopyProcessors { get; }

    /// <summary>
    /// The processors that turn pasted text into objects.
    /// </summary>
    IPluginRegistry<IPasteProcessor> PasteProcessors { get; }

    /// <summary>
    /// The processors that fix up pasted assets.
    /// </summary>
    IPluginRegistry<IAssetPostPasteProcessor> PostPasteProcessors { get; }

    /// <summary>
    /// The tools of the views of the game editors (the scene and prefab editors): each view creates one instance of each
    /// type. The host registers the tools that the plugin's assembly declares; a plugin can register more.
    /// </summary>
    IPluginRegistry<Type> EditorViewToolTypes { get; }

    /// <summary>
    /// The editor settings keys of the plugin, registered with their container while the plugin is loaded.
    /// </summary>
    IPluginRegistry<SettingsKey> SettingsKeys { get; }

    /// <summary>
    /// The settings that the plugin adds to the properties of the packages.
    /// </summary>
    IPluginRegistry<PackageSettingsEntry> ProfileSettings { get; }

    /// <summary>
    /// The panels of the plugin, shown in the main window and listed in its View menu. The plugin owns them: when it leaves,
    /// they are unregistered and their open instances close. An editor that shows no panels (the WPF Game Studio) accepts them
    /// and shows nothing.
    /// </summary>
    IPluginRegistry<PanelDescriptor> Panels { get; }

    /// <summary>
    /// The view type of each view model type, in the editor's toolkit: the views of the plugin's panels (and, later, of its
    /// other contributions). The toolkit companion of a plugin fills it; a view model with no view in this editor shows
    /// "no view for this editor". A registration for a base type covers the types derived from it.
    /// </summary>
    IPluginMap<Type, Type> ViewTypes { get; }

    /// <summary>
    /// The commands of the plugin, placed in the menus, the context menus and the toolbar of the editor at named locations
    /// (<see cref="Commands.CommandLocations"/>), with their shortcuts. The plugin owns them: when it leaves, they go. An editor
    /// that shows no plugin commands (the WPF Game Studio) accepts them and shows nothing.
    /// </summary>
    IPluginRegistry<Commands.CommandDescriptor> Commands { get; }

    /// <summary>
    /// The widgets of the plugin in the main toolbar, after the command buttons, shown with the views of
    /// <see cref="ViewTypes"/>. The plugin owns them: when it leaves, they go and their view models are destroyed. An editor
    /// that shows no plugin widgets (the WPF Game Studio) accepts them and shows nothing.
    /// </summary>
    IPluginRegistry<Commands.ToolbarWidgetDescriptor> ToolbarWidgets { get; }

    /// <summary>
    /// A registry that another layer of the editor declares (<see cref="EditorPluginHost.DeclareRegistry{T}"/>), for the
    /// contributions that this context does not name; the declaring layer usually gives it a typed extension method.
    /// </summary>
    /// <typeparam name="T">The type of the contributions.</typeparam>
    /// <exception cref="InvalidOperationException">No layer of this editor declares a registry of <typeparamref name="T"/>.</exception>
    IPluginRegistry<T> GetRegistry<T>() where T : class;
}
