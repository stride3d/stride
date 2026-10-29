// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Views;
using Stride.Core.Settings;

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
}
