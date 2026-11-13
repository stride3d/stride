// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Editor.Build;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Reflection;
using Stride.Core.Storage;
using Stride.Editor;
using Stride.Editor.Build;
using Stride.Editor.Thumbnails;
using Stride.Engine.Gizmos;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Editor.SceneEditor;
using Stride.Assets.Editor.ViewModels.CopyPasteProcessors;

namespace Stride.Assets.Editor;

/// <summary>
/// The editor plugin of the Stride engine assets: it wires the build, thumbnail and settings services of a session, registers the node
/// presenter commands and updaters of the property grids, the copy/paste processors, and keeps the registries of the gizmos, entity
/// factories and add-asset policies declared by the loaded asset assemblies.
/// </summary>
/// <remarks>
/// The preview service and the views belong to the toolkit companion (<c>Stride.Assets.Editor.Avalonia</c>).
/// A default plugin of the cross-platform editor: its host registers it (<c>Stride.Assets.Editor.Avalonia</c>), the discovery skips it.
/// </remarks>
[EditorDefaultPlugin]
public sealed class StrideEditorPlugin : StrideAssetsPlugin
{
    /// <summary>
    /// The name of the folder of the fallback build cache, under <see cref="Core.Assets.Editor.Settings.EditorSettings.FallbackBuildCacheDirectory"/>.
    /// </summary>
    public const string EditorName = "Stride Game Studio";

    /// <summary>Delete cache blobs unused longer than this (Y). Must stay above <see cref="FileOdbBackend.TouchThrottle"/> (X).</summary>
    private static readonly TimeSpan EvictionAge = TimeSpan.FromDays(14);

    /// <summary>Run the sweep at most this often across launches, tracked via the marker file's mtime.</summary>
    private static readonly TimeSpan GcInterval = TimeSpan.FromHours(24);

    private static readonly ObservableList<EntityFactoryCategory> EntityFactoryCategoryList = [];
    private static bool assemblyHooksRegistered;

    /// <summary>
    /// Whether <see cref="InitializeSession(ISessionPluginContext)"/> creates the thumbnail service. Tests without a graphics device turn it off.
    /// </summary>
    public bool EnableThumbnailService { get; set; } = true;

    /// <summary>
    /// The entity factories ("Add entity" menu) of every asset assembly, grouped by category.
    /// </summary>
    /// <remarks>
    /// The list changes on the thread that registers the assembly. A view that binds it must marshal the change to its UI thread.
    /// </remarks>
    public static IReadOnlyObservableList<EntityFactoryCategory> EntityFactoryCategories => EntityFactoryCategoryList;

    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        context.ProfileSettings.Register(new PackageSettingsEntry(GameUserSettings.Effect.EffectCompilation, TargetPackage.Executable));
        context.ProfileSettings.Register(new PackageSettingsEntry(GameUserSettings.Effect.RecordUsedEffects, TargetPackage.Executable));

        // The settings of the scene and script editors, declared in the shared project for both editors
        foreach (var settingsKey in EditorSettingsAttribute.GetKeys(typeof(SceneEditorSettings)).Concat(EditorSettingsAttribute.GetKeys(typeof(ScriptEditorSettings))))
            context.SettingsKeys.Register(settingsKey);

        context.CopyProcessors.Register(new EntityComponentCopyProcessor());
        context.PasteProcessors.Register(new EntityComponentPasteProcessor());
        context.PasteProcessors.Register(new EntityHierarchyPasteProcessor());
        context.PostPasteProcessors.Register(new ScenePostPasteProcessor());

        // The registries of the asset assemblies are process-wide: they follow the assembly registry from the first session on
        if (assemblyHooksRegistered)
            return;
        assemblyHooksRegistered = true;

        AssemblyRegistry.AssemblyRegistered += (_, e) =>
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                OnRegisteredAssetAssembly(e.Assembly);
        };
        AssemblyRegistry.AssemblyUnregistered += (_, e) =>
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                OnUnregisteredAssetAssembly(e.Assembly);
        };

        foreach (var assembly in AssetRegistry.AssetAssemblies)
            OnRegisteredAssetAssembly(assembly);
    }

    /// <inheritdoc />
    public override void InitializeSession(ISessionPluginContext context)
    {
        base.InitializeSession(context);

        var session = context.Session;
        var sessionViewModel = (SessionViewModel)session;
        var buildDirectory = GetBuildDirectory(session);

        // Reclaim stale cache entries before the builder opens the DB (nothing holds it yet, so no coordination needed).
        CollectBuildCache(buildDirectory);

        var settingsProvider = new GameSettingsProviderService(session);
        context.Services.Register(settingsProvider);

        // The scene and prefab editors show the toolbar of the Avalonia view as their view options
        context.Services.Register(new EditorViewToolbarFactory());

        var builderService = new GameStudioBuilderService(sessionViewModel, settingsProvider, new StrideShaderImporter(), buildDirectory);
        context.Services.Register(builderService);
        context.Services.Register(builderService.Database); // TODO: this should be removed, the AssetBuilderService is reachable from anywhere now

        if (EnableThumbnailService)
        {
            var thumbnailService = new GameStudioThumbnailService(sessionViewModel, settingsProvider, builderService);
            context.Services.Register(thumbnailService);
        }

        // commands
        context.NodePresenterCommands.Register(new FetchEntityCommand());
        context.NodePresenterCommands.Register(new SetEntityReferenceCommand());
        context.NodePresenterCommands.Register(new SetComponentReferenceCommand());
        context.NodePresenterCommands.Register(new SetSymbolReferenceCommand());
        context.NodePresenterCommands.Register(new SkeletonNodePreserveAllCommand());
        // Opens the curve editor of the editor, if it has one (ICurveEditorService)
        context.NodePresenterCommands.Register(new EditCurveCommand(sessionViewModel));

        // updaters
        context.NodePresenterUpdaters.Register(new AnimationAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new CameraSlotNodeUpdater());
        context.NodePresenterUpdaters.Register(new EntityHierarchyAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new GameSettingsAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new GraphicsCompositorAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new FXAAEffectNodeUpdater());
        context.NodePresenterUpdaters.Register(new MaterialAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new ModelAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new ModelComponentNodeUpdater());
        context.NodePresenterUpdaters.Register(new ModelNodeLinkNodeUpdater());
        context.NodePresenterUpdaters.Register(new SkeletonAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new SpriteSheetAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new TextureAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new UnloadableObjectPropertyNodeUpdater());

        // Extra packages to display in "add reference" dialog
        sessionViewModel.SuggestedPackages.Add(new PackageName(typeof(Stride.Engine.EntityComponent).Assembly.GetName().Name!, new PackageVersion(StrideVersion.NuGetVersion)));
        // Plugins are not referenced by the editor, so their ids are strings
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.Audio", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.Particles", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.Navigation", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.Physics", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.BepuPhysics", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.Heightfield", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.Video", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.Voxels", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.SpriteStudio.Runtime", new PackageVersion(StrideVersion.NuGetVersion)));
        sessionViewModel.SuggestedPackages.Add(new PackageName("Stride.UI", new PackageVersion(StrideVersion.NuGetVersion)));
    }

    /// <summary>
    /// The directory of the asset build database of the session: under the package that owns the game settings when there is one,
    /// otherwise the fallback cache of the editor.
    /// </summary>
    internal static string GetBuildDirectory(ISessionViewModel session)
    {
        var fallbackDirectory = UPath.Combine(Core.Assets.Editor.Settings.EditorSettings.FallbackBuildCacheDirectory, new UDirectory(EditorName)).ToOSPath();
        var buildDirectory = fallbackDirectory;
        try
        {
            var currentPackage = (session.CurrentProject ?? session.LocalPackages.FirstOrDefault())?.Package;
            if (currentPackage != null)
            {
                // Editor build DB lives under the shared game library (the package that owns
                // GameSettings), not the startup platform head — so adding/removing/regenerating a
                // head can't relocate or corrupt it. FindAsset resolves through dependencies, so
                // from a head this lands on its game library; falls back to the current package.
                var buildPackage = currentPackage.FindAsset(GameSettingsAsset.GameSettingsLocation)?.Package ?? currentPackage;
                buildDirectory = Path.Combine(buildPackage.FullPath.GetFullDirectory().ToOSPath(), "obj", "stride", "assetbuild", "data");
            }

            // Attempt to create the directory to ensure it is valid.
            if (!Directory.Exists(buildDirectory))
                Directory.CreateDirectory(buildDirectory);
        }
        catch (Exception)
        {
            buildDirectory = fallbackDirectory;
        }
        return buildDirectory;
    }

    /// <summary>
    /// Startup GC for the shared asset-build cache. A blob survives if either source keeps it live:
    /// <list type="bullet">
    /// <item>fresh mtime — the editor touches its working set on hit and re-touches it (<see cref="ObjectDatabase.Touch"/>);</item>
    /// <item>listed in a current config's <c>index.&lt;config&gt;.closure</c> — each CLI/head build writes its full
    /// reachable set (command entries + output blobs), dated by the file's mtime.</item>
    /// </list>
    /// Everything else is deleted: content past <see cref="EvictionAge"/> unreferenced by a fresh index/closure,
    /// and stale configs' index/closure files. Marker-gated to once per <see cref="GcInterval"/>, age-only, best-effort.
    /// </summary>
    internal static void CollectBuildCache(string buildDirectory)
    {
        try
        {
            if (!Directory.Exists(buildDirectory))
                return;

            var marker = Path.Combine(buildDirectory, "gc.stamp");
            if (File.Exists(marker) && DateTime.UtcNow - File.GetLastWriteTimeUtc(marker) < GcInterval)
                return;

            // The content store lives under db/; blobs + command entries are in its 2-hex-char subdirs.
            // bundles/, tmp/ and the index files (all also under db/) fall outside those, so they're skipped.
            var contentRoot = Path.Combine(buildDirectory, "db");
            if (!Directory.Exists(contentRoot))
                return;

            // Y >= X floor: a live file's mtime is at most X stale (touch throttle), so never evict within X.
            var age = EvictionAge > FileOdbBackend.TouchThrottle ? EvictionAge : FileOdbBackend.TouchThrottle;
            var cutoff = DateTime.UtcNow - age;

            // Keep blobs referenced by a still-current index/closure; superseded/orphaned blobs (in
            // none) are left to age out below.
            var referenced = CollectReferencedBlobs(contentRoot, cutoff);

            foreach (var dir in Directory.EnumerateDirectories(contentRoot))
            {
                if (Path.GetFileName(dir).Length != 2)
                    continue;

                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    if (referenced.Contains(file))
                        continue;

                    try
                    {
                        if (File.GetLastWriteTimeUtc(file) < cutoff)
                            File.Delete(file);
                    }
                    catch (IOException) { }               // in use / racing another process — skip
                    catch (UnauthorizedAccessException) { }
                }
            }

            File.WriteAllText(marker, string.Empty);
        }
        catch (Exception)
        {
            // Best-effort reclaim; a failure here must not stop the session from opening.
        }
    }

    /// <summary>
    /// Reads the index/closure files at the content-store root and returns the absolute blob paths they
    /// reference (reconstructed via the db/&lt;id[0..1]&gt;/&lt;id[2..]&gt; layout). A per-config
    /// <c>index.*</c> carries its build time in its mtime: once older than <paramref name="cutoff"/> the
    /// config is stale, so its index/closure files are deleted and their blobs left to age out below.
    /// </summary>
    private static HashSet<string> CollectReferencedBlobs(string contentRoot, DateTime cutoff)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var indexFile in Directory.EnumerateFiles(contentRoot))
        {
            try
            {
                if (Path.GetFileName(indexFile).StartsWith("index.", StringComparison.OrdinalIgnoreCase)
                    && File.GetLastWriteTimeUtc(indexFile) < cutoff)
                {
                    File.Delete(indexFile);
                    continue;
                }

                foreach (var raw in File.ReadLines(indexFile))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#')
                        continue;

                    // Each line ends with the object id (a plain closure line is just the id; an index
                    // line is "<url> <id>"), so the trailing whitespace-delimited token is the id.
                    var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    var id = parts[^1];
                    if (id.Length == 32)
                        referenced.Add(Path.Combine(contentRoot, id[..2], id[2..]));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return referenced;
    }

    /// <summary>
    /// Gizmos, add-asset policies and entity factories of <paramref name="assembly"/>, discovered through its source-generated scan index.
    /// </summary>
    private static void OnRegisteredAssetAssembly(Assembly assembly)
    {
        GizmoTypeRegistry.RegisterAssembly(assembly);
        AddAssetPolicyRegistry.RegisterAssembly(assembly);

        var factoryTypes = AssemblyRegistry.GetScanTypes(assembly, typeof(IEntityFactory)).ToArray();
        lock (EntityFactoryCategoryList)
            RegisterEntityFactories(factoryTypes);
    }

    private static void OnUnregisteredAssetAssembly(Assembly assembly)
    {
        GizmoTypeRegistry.UnregisterAssembly(assembly);
        AddAssetPolicyRegistry.UnregisterAssembly(assembly);

        var factoryTypes = AssemblyRegistry.GetScanTypes(assembly, typeof(IEntityFactory)).ToArray();
        lock (EntityFactoryCategoryList)
            UnregisterEntityFactories(factoryTypes);
    }

    private static bool IsEntityFactory(Type type)
    {
        return type.IsAssignableTo(typeof(IEntityFactory)) && type.IsClass && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static void RegisterEntityFactories(Type[] types)
    {
        foreach (var factoryType in types.Where(IsEntityFactory))
        {
            var display = factoryType.GetCustomAttribute<DisplayAttribute>();
            if (display?.Category == null)
                continue;

            var category = EntityFactoryCategoryList.FirstOrDefault(x => x.Name == display.Category);
            if (category == null)
            {
                category = new EntityFactoryCategory(display.Category);
                var index = 0;
                while (index < EntityFactoryCategoryList.Count && ((IComparable<EntityFactoryCategory>)EntityFactoryCategoryList[index]).CompareTo(category) < 0)
                    ++index;
                EntityFactoryCategoryList.Insert(index, category);
            }

            var instance = (IEntityFactory)Activator.CreateInstance(factoryType)!;
            // We use int.MaxValue / 2 to give enough space to all factories that do not have an Order value
            category.AddFactory(instance, display.Name ?? factoryType.Name, display.Order ?? int.MaxValue / 2);
        }
    }

    private static void UnregisterEntityFactories(Type[] types)
    {
        foreach (var category in EntityFactoryCategoryList.ToList())
        {
            foreach (var factory in category.Factories.Where(x => types.Contains(x.Factory.GetType())).ToList())
                category.Factories.Remove(factory);

            if (category.Factories.Count == 0)
                EntityFactoryCategoryList.Remove(category);
        }
    }
}
