// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Views;
using Stride.Core.Settings;
using Stride.Editor;

#nullable enable

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// What the editor's plugin host registers for one plugin, without a session: what the plugin's assembly declares with
/// attributes, then what the plugin registers in <see cref="AssetsPlugin.Initialize(IEditorPluginContext)"/>, after
/// <see cref="AssetsPlugin.InitializePlugin"/>, in the host's order.
/// </summary>
public sealed class PluginRegistrations : IEditorPluginContext
{
    private readonly Dictionary<Type, object> legacyTypeImages = [];

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

    public ListRecorder<Type> EditorViewToolTypes { get; } = new();

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

    IPluginRegistry<Type> IEditorPluginContext.EditorViewToolTypes => EditorViewToolTypes;

    IPluginRegistry<SettingsKey> IEditorPluginContext.SettingsKeys => SettingsKeys;

    IPluginRegistry<PackageSettingsEntry> IEditorPluginContext.ProfileSettings => ProfileSettings;

    /// <summary>
    /// The registrations of <paramref name="plugin"/>, in an editor whose editor views implement <paramref name="editorViewType"/>.
    /// </summary>
    public static PluginRegistrations Of(AssetsPlugin plugin, Type editorViewType)
    {
        var registrations = new PluginRegistrations(plugin);
        var typeImages = AssetsEditorPlugin.TypeImagesDictionary.ToDictionary();
        plugin.InitializePlugin(registrations.Logger);
        new Scanner(editorViewType).Scan(plugin, registrations);
        plugin.Initialize(registrations);
        // The type images that the plugin set in the legacy static dictionary, which the editors read too
        foreach (var (type, image) in AssetsEditorPlugin.TypeImagesDictionary)
        {
            if (!typeImages.TryGetValue(type, out var previous) || !ReferenceEquals(previous, image))
                registrations.legacyTypeImages[type] = image;
        }
        return registrations;
    }

    /// <summary>
    /// The registrations as text, one per line, to compare the registrations of a plugin before and after a change: the maps
    /// sorted by key, the lists in registration order. The settings that the plugin adds to its legacy
    /// <see cref="AssetsEditorPlugin.ProfileSettings"/> list, and the images it sets in the legacy static
    /// <see cref="AssetsEditorPlugin.TypeImagesDictionary"/>, count as registered, as the editors read them too.
    /// </summary>
    /// <param name="sortTemplateProviders">Whether to sort the template providers: a WPF resource dictionary lists its values
    /// in hash order, which changes from one process to the next.</param>
    public string Describe(bool sortTemplateProviders = false)
    {
        var builder = new StringBuilder();
        builder.Append("Plugin: ").AppendLine(Plugin.GetType().FullName);
        Section(builder, nameof(AssetViewModelTypes), Sorted(AssetViewModelTypes));
        Section(builder, nameof(AssetEditorViewModelTypes), Sorted(AssetEditorViewModelTypes));
        Section(builder, nameof(AssetEditorViewTypes), Sorted(AssetEditorViewTypes));
        Section(builder, nameof(AssetPreviewViewModelTypes), Sorted(AssetPreviewViewModelTypes));
        Section(builder, nameof(AssetPreviewViewTypes), Sorted(AssetPreviewViewTypes));
        Section(builder, nameof(PrimitiveTypes), PrimitiveTypes.Select(Name).Distinct().Order(StringComparer.Ordinal));
        Section(builder, nameof(EnumImages), EnumImages.Select(x => $"{x.Key.GetType().FullName}.{x.Key} -> {x.Value.GetType().FullName}").Order(StringComparer.Ordinal));
        Section(builder, nameof(TypeImages), TypeImages.Concat(legacyTypeImages).Select(x => $"{Name(x.Key)} -> {x.Value.GetType().FullName}").Order(StringComparer.Ordinal));
        var templateProviders = TemplateProviders.Select(x => $"{Name(x.GetType())} {x.Name}");
        Section(builder, nameof(TemplateProviders), sortTemplateProviders ? templateProviders.Order(StringComparer.Ordinal) : templateProviders);
        Section(builder, nameof(CopyProcessors), CopyProcessors.Select(x => Name(x.GetType())));
        Section(builder, nameof(PasteProcessors), PasteProcessors.Select(x => Name(x.GetType())));
        Section(builder, nameof(PostPasteProcessors), PostPasteProcessors.Select(x => Name(x.GetType())));
        // Only for a plugin with tools: the recordings of the others predate the tools
        if (EditorViewToolTypes.Count > 0)
            Section(builder, nameof(EditorViewToolTypes), EditorViewToolTypes.Select(Name).Order(StringComparer.Ordinal));
        Section(builder, nameof(SettingsKeys), SettingsKeys.Select(x => x.Name.ToString()));
        var profileSettings = (Plugin as AssetsEditorPlugin)?.ProfileSettings.Concat(ProfileSettings) ?? ProfileSettings;
        Section(builder, nameof(ProfileSettings), profileSettings.Select(x => $"{x.SettingsKey.Name} ({x.TargetPackage})"));
        return builder.ToString();
    }

    internal static string Name(Type type) => type.FullName ?? type.Name;

    internal static void Section(StringBuilder builder, string name, IEnumerable<string> lines)
    {
        builder.Append(name).AppendLine(":");
        foreach (var line in lines)
            builder.Append("  ").AppendLine(line);
    }

    private static IEnumerable<string> Sorted(IReadOnlyDictionary<Type, Type> map)
    {
        return map.Select(x => $"{Name(x.Key)} -> {Name(x.Value)}").Order(StringComparer.Ordinal);
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
