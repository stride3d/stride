// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// A plugin service that resolves the types registered by the plugins it is given, the way the editor shell's service does:
/// the view model of an asset type (or of a base type) and the view model of a preview type (or of its generic definition).
/// </summary>
public sealed class TestPluginService : IAssetsPluginService
{
    private readonly List<AssetsPlugin> plugins;
    private readonly Dictionary<Type, Type> assetViewModelTypes = [];
    private readonly Dictionary<Type, Type> previewViewModelTypes = [];
    private readonly Dictionary<Type, Type> previewViewTypes = [];
    private readonly Dictionary<Type, Type> editorViewModelTypes = [];
    private readonly Dictionary<Type, Type> editorViewTypes = [];
    private readonly List<Type> primitiveTypes = [];

    public TestPluginService(params AssetsPlugin[] plugins)
    {
        this.plugins = [.. plugins];
    }

    public IReadOnlyList<AssetsPlugin> Plugins => plugins;

    public IReadOnlyDictionary<Type, Type> AssetViewModelTypes => assetViewModelTypes;

    public IReadOnlyDictionary<Type, Type> PreviewViewModelTypes => previewViewModelTypes;

    public IReadOnlyList<Type> PrimitiveTypes => primitiveTypes;

    public int Registrations { get; private set; }

    public IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session) => primitiveTypes;

    public IEnumerable<PackageSettingsEntry> GetProfileSettings() => plugins.OfType<AssetsEditorPlugin>().SelectMany(x => x.ProfileSettings);

    public bool HasImagesForEnum(ISessionViewModel? session, Type enumType) => false;

    public object? GetImageForEnum(ISessionViewModel? session, object value) => null;

    public bool HasEditorView(ISessionViewModel session, Type viewModelType) => GetEditorViewModelType(viewModelType) is { } editor && GetEditorViewType(editor) is not null;

    public Type? GetAssetViewModelType(Type assetType) => Resolve(assetViewModelTypes, assetType);

    public Type? GetEditorViewModelType(Type viewModelType) => Resolve(editorViewModelTypes, viewModelType);

    public Type? GetEditorViewType(Type editorViewModelType) => Resolve(editorViewTypes, editorViewModelType);

    public Type? GetPreviewViewModelType(Type previewType) => Resolve(previewViewModelTypes, previewType);

    public Type? GetPreviewViewType(Type previewType) => Resolve(previewViewTypes, previewType);

    public void RegisterSession(ISessionViewModel session, ILogger logger)
    {
        Registrations++;
        foreach (var plugin in plugins)
        {
            plugin.InitializePlugin(logger);
            plugin.RegisterAssetViewModelTypes(assetViewModelTypes);
            plugin.RegisterPrimitiveTypes(primitiveTypes);
            if (plugin is AssetsEditorPlugin editorPlugin)
            {
                editorPlugin.RegisterAssetPreviewViewModelTypes(previewViewModelTypes);
                editorPlugin.RegisterAssetPreviewViewTypes(previewViewTypes);
                editorPlugin.RegisterAssetEditorViewModelTypes(editorViewModelTypes);
                editorPlugin.RegisterAssetEditorViewTypes(editorViewTypes);
            }
        }
    }

    public void InitializeSession(ISessionViewModel session)
    {
        foreach (var plugin in plugins)
            plugin.InitializeSession(session);
    }

    /// <summary>
    /// The registered type for <paramref name="type"/>, one of its base types, or the generic definition of either.
    /// </summary>
    private static Type? Resolve(Dictionary<Type, Type> registry, Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (registry.TryGetValue(current, out var result))
                return result;
            if (current.IsGenericType && registry.TryGetValue(current.GetGenericTypeDefinition(), out result))
                return result;
        }
        return null;
    }
}
