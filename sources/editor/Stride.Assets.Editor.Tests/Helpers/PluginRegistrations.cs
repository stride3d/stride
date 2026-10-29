// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Views;
using Stride.Core.Settings;
using Stride.Editor;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// What the editor's plugin host registers for one plugin, without a session: what the plugin's assembly declares with
/// attributes, then what the plugin registers in <see cref="AssetsPlugin.Initialize(IEditorPluginContext)"/>.
/// </summary>
public sealed class PluginRegistrations : IEditorPluginContext
{
    private PluginRegistrations(AssetsPlugin plugin)
    {
        Plugin = plugin;
    }

    public AssetsPlugin Plugin { get; }

    public ISessionViewModel Session => null!;

    public ILogger Logger { get; } = new LoggerResult();

    public Recorder<Type, Type> AssetViewModelTypes { get; } = new();

    public Recorder<Type, Type> AssetEditorViewModelTypes { get; } = new();

    public Recorder<Type, Type> AssetEditorViewTypes { get; } = new();

    public Recorder<Type, Type> AssetPreviewViewModelTypes { get; } = new();

    public Recorder<Type, Type> AssetPreviewViewTypes { get; } = new();

    public ListRecorder<Type> PrimitiveTypes { get; } = new();

    public Recorder<object, object> EnumImages { get; } = new();

    public Recorder<Type, object> TypeImages { get; } = new();

    public ListRecorder<ITemplateProvider> TemplateProviders { get; } = new();

    public ListRecorder<ICopyProcessor> CopyProcessors { get; } = new();

    public ListRecorder<IPasteProcessor> PasteProcessors { get; } = new();

    public ListRecorder<IAssetPostPasteProcessor> PostPasteProcessors { get; } = new();

    public ListRecorder<SettingsKey> SettingsKeys { get; } = new();

    public ListRecorder<PackageSettingsEntry> ProfileSettings { get; } = new();

    IPluginMap<Type, Type> IEditorPluginContext.AssetViewModelTypes => AssetViewModelTypes;

    IPluginMap<Type, Type> IEditorPluginContext.AssetEditorViewModelTypes => AssetEditorViewModelTypes;

    IPluginMap<Type, Type> IEditorPluginContext.AssetEditorViewTypes => AssetEditorViewTypes;

    IPluginMap<Type, Type> IEditorPluginContext.AssetPreviewViewModelTypes => AssetPreviewViewModelTypes;

    IPluginMap<Type, Type> IEditorPluginContext.AssetPreviewViewTypes => AssetPreviewViewTypes;

    IPluginRegistry<Type> IEditorPluginContext.PrimitiveTypes => PrimitiveTypes;

    IPluginMap<object, object> IEditorPluginContext.EnumImages => EnumImages;

    IPluginMap<Type, object> IEditorPluginContext.TypeImages => TypeImages;

    IPluginRegistry<ITemplateProvider> IEditorPluginContext.TemplateProviders => TemplateProviders;

    IPluginRegistry<ICopyProcessor> IEditorPluginContext.CopyProcessors => CopyProcessors;

    IPluginRegistry<IPasteProcessor> IEditorPluginContext.PasteProcessors => PasteProcessors;

    IPluginRegistry<IAssetPostPasteProcessor> IEditorPluginContext.PostPasteProcessors => PostPasteProcessors;

    IPluginRegistry<SettingsKey> IEditorPluginContext.SettingsKeys => SettingsKeys;

    IPluginRegistry<PackageSettingsEntry> IEditorPluginContext.ProfileSettings => ProfileSettings;

    /// <summary>
    /// The registrations of <paramref name="plugin"/>, in an editor whose editor views implement <paramref name="editorViewType"/>.
    /// </summary>
    public static PluginRegistrations Of(AssetsPlugin plugin, Type editorViewType)
    {
        var registrations = new PluginRegistrations(plugin);
        new Scanner(editorViewType).Scan(plugin, registrations);
        plugin.Initialize(registrations);
        return registrations;
    }

    public sealed class Recorder<TKey, TValue> : Dictionary<TKey, TValue>, IPluginMap<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        // The same registration twice is not an error, as in the host
        public void Register(TKey key, TValue value)
        {
            if (!TryGetValue(key, out var existing) || !Equals(existing, value))
                Add(key, value);
        }
    }

    public sealed class ListRecorder<T> : List<T>, IPluginRegistry<T>
        where T : notnull
    {
        public void Register(T item) => Add(item);
    }

    // The attribute scans of the editors' host
    private sealed class Scanner(Type editorViewType) : StrideEditorPluginHost([])
    {
        protected override Type AssetEditorViewModelType => typeof(object);

        protected override Type AssetEditorViewType => editorViewType;

        public void Scan(AssetsPlugin plugin, IEditorPluginContext context) => ScanPlugin(plugin, context);

        protected override bool AddTemplateProvider(ISessionViewModel session, ITemplateProvider provider) => false;

        protected override void RemoveTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
        {
        }
    }
}
