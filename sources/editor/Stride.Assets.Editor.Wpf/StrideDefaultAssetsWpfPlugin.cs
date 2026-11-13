// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Assets.Presentation.AssetEditors.AssetHighlighters;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.NodePresenters.Commands;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Presentation.NodePresenters.Updaters;
using Stride.Assets.Presentation.SceneEditor.Services;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Templates;
using Stride.Editor;
using Stride.Engine;
using Stride.Core.Assets.Templates;
using Stride.Core.Packages;
using Stride.Core.Serialization;
using Stride.Engine.Gizmos;
using Stride.Editor.Annotations;
using Stride.Editor.Preview.View;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Assets.Editor.ViewModels.CopyPasteProcessors;

namespace Stride.Assets.Presentation
{
    public sealed class StrideDefaultAssetsWpfPlugin : StrideAssetsWpfPlugin
    {
        private static ResourceDictionary imageDictionary;
        private static ResourceDictionary animationPropertyTemplateDictionary;
        private static ResourceDictionary entityPropertyTemplateDictionary;
        private static ResourceDictionary materialPropertyTemplateDictionary;
        private static ResourceDictionary skeletonTemplateDictionary;
        private static ResourceDictionary spriteFontTemplateDictionary;
        private static ResourceDictionary graphicsCompositorTemplateDictionary;
        private static ResourceDictionary visualScriptingTemplateDictionary;
        private static ResourceDictionary visualScriptingGraphTemplatesDictionary;
        // Replaced, never changed: the editor games read them on their threads while an assembly registers
        private static volatile IReadOnlyDictionary<Type, Type> assetHighlighterTypes = new Dictionary<Type, Type>();
        private static readonly object TypeMapsLock = new object();

        public static IReadOnlyDictionary<Type, Type> AssetHighlighterTypesDictionary => assetHighlighterTypes;

        private static readonly ObservableList<EntityFactoryCategory> EntityFactoryCategoryList = new ObservableList<EntityFactoryCategory>();

        /// <summary>
        /// The entity factories ("Add entity" menu) of every asset assembly, grouped by category.
        /// </summary>
        public static IReadOnlyObservableList<EntityFactoryCategory> EntityFactoryCategories => EntityFactoryCategoryList;

        public StrideDefaultAssetsWpfPlugin()
        {
            LoadDefaultTemplates();
        }

        public static void LoadDefaultTemplates() => StrideDefaultTemplates.Load();

        /// <summary>
        /// The asset view models, editors and preview view models that this editor shares with the cross-platform one.
        /// </summary>
        /// <remarks>
        /// They live in the agnostic assemblies, whose plugins are the cross-platform editor's default plugins (this editor does
        /// not load them), and next to cross-platform types that this editor has its own versions of (the script and visual
        /// script editors): this plugin registers them one by one.
        /// </remarks>
        internal static readonly Type[] SharedViewModelTypes =
        {
            typeof(AnimationViewModel), typeof(EffectLogViewModel), typeof(GameSettingsViewModel), typeof(GraphicsCompositorViewModel),
            typeof(MaterialViewModel), typeof(ModelViewModel), typeof(PrecompiledSpriteFontViewModel), typeof(PrefabViewModel),
            typeof(ProceduralModelViewModel), typeof(SceneViewModel), typeof(SkeletonViewModel), typeof(SkyboxViewModel),
            typeof(SpriteFontViewModel), typeof(SpriteSheetViewModel), typeof(VisualScriptViewModel),
            typeof(GraphicsCompositorEditorViewModel), typeof(PrefabEditorViewModel), typeof(SceneEditorViewModel), typeof(SpriteSheetEditorViewModel),
            typeof(AnimationPreviewViewModel), typeof(EntityPreviewViewModel), typeof(MaterialPreviewViewModel), typeof(ModelPreviewViewModel),
            typeof(PrecompiledSpriteFontPreviewViewModel), typeof(ProceduralModelPreviewViewModel), typeof(SkyboxPreviewViewModel),
            typeof(SpriteFontPreviewViewModel), typeof(SpritePreviewViewModel), typeof(SpriteSheetPreviewViewModel), typeof(TexturePreviewViewModel),
        };

        /// <inheritdoc />
        public override void Initialize(IEditorPluginContext context)
        {
            base.Initialize(context);

            foreach (var type in SharedViewModelTypes)
            {
                if (type.GetCustomAttribute<AssetViewModelAttribute>() is { } assetViewModel)
                    context.AssetViewModelTypes.Register(assetViewModel.AssetType, type);
                if (type.GetCustomAttribute<AssetEditorViewModelAttribute>() is { } editorViewModel)
                    context.AssetEditorViewModelTypes.Register(editorViewModel.ViewModelType, type);
                if (type.GetCustomAttribute<AssetPreviewViewModelAttribute>() is { } previewViewModel)
                    context.AssetPreviewViewModelTypes.Register(previewViewModel.AssetPreviewType, type);
            }

            context.ProfileSettings.Register(new PackageSettingsEntry(GameUserSettings.Effect.EffectCompilation, TargetPackage.Executable));
            context.ProfileSettings.Register(new PackageSettingsEntry(GameUserSettings.Effect.RecordUsedEffects, TargetPackage.Executable));
            context.PrimitiveTypes.Register(typeof(AssetReference));
            context.PrimitiveTypes.Register(typeof(UrlReferenceBase));

            imageDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/ImageDictionary.xaml", UriKind.RelativeOrAbsolute));
            animationPropertyTemplateDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/AnimationPropertyTemplates.xaml", UriKind.RelativeOrAbsolute));
            entityPropertyTemplateDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/EntityPropertyTemplates.xaml", UriKind.RelativeOrAbsolute));
            materialPropertyTemplateDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/MaterialPropertyTemplates.xaml", UriKind.RelativeOrAbsolute));
            skeletonTemplateDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/SkeletonPropertyTemplates.xaml", UriKind.RelativeOrAbsolute));
            spriteFontTemplateDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/SpriteFontPropertyTemplates.xaml", UriKind.RelativeOrAbsolute));
            graphicsCompositorTemplateDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/GraphicsCompositorTemplates.xaml", UriKind.RelativeOrAbsolute));
            visualScriptingTemplateDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/View/VisualScriptingTemplates.xaml", UriKind.RelativeOrAbsolute));
            visualScriptingGraphTemplatesDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/AssetEditors/VisualScriptEditor/Views/GraphTemplates.xaml", UriKind.RelativeOrAbsolute));

            // Make Visual Script colors available to StaticResourceConverter
            Application.Current.Resources.MergedDictionaries.Add(imageDictionary);

            // Make script editor styles and icons available to StaticResourceConverter
            Application.Current.Resources.MergedDictionaries.Add((ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/AssetEditors/ScriptEditor/Resources/Icons.xaml", UriKind.RelativeOrAbsolute)));
            Application.Current.Resources.MergedDictionaries.Add((ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Assets.Editor.Wpf;component/AssetEditors/ScriptEditor/Resources/ThemeScriptEditor.xaml", UriKind.RelativeOrAbsolute)));

            // RoslynPad's light-bulb menu derives from ContextMenu, and WPF implicit styles aren't
            // inherited by derived types - so register our themed ContextMenu style under that type.
            var bulbMenuType = typeof(RoslynPad.Editor.AvalonEditTextContainer).Assembly
                .GetType("RoslynPad.Editor.ContextActionsBulbContextMenu");
            if (bulbMenuType != null && Application.Current.TryFindResource(typeof(System.Windows.Controls.ContextMenu)) is Style contextMenuStyle)
            {
                Application.Current.Resources[bulbMenuType] = new Style(bulbMenuType, contextMenuStyle);
            }

            AssemblyRegistry.AssemblyRegistered += (sender, e) =>
            {
                EntityComponentMetadata.SetDisplayFallback(e.Assembly.GetTypes());

                if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                {
                    OnRegisteredAssetAssembly(e.Assembly);
                }
            };

            AssemblyRegistry.AssemblyUnregistered += (sender, e) =>
            {
                if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                {
                    OnUnregisteredAssetAssembly(e.Assembly);
                }
            };

            EntityComponentMetadata.SetDisplayFallback(typeof(EntityComponent).GetInheritedInstantiableTypes());

            foreach (var assembly in AssetRegistry.AssetAssemblies)
                OnRegisteredAssetAssembly(assembly);

            // The type-keyed entries are type images; a later dictionary replaces the image of an earlier one
            var typeImages = new Dictionary<Type, object>();
            foreach (var dictionary in new[]
                     {
                         imageDictionary, animationPropertyTemplateDictionary, entityPropertyTemplateDictionary, materialPropertyTemplateDictionary,
                         skeletonTemplateDictionary, spriteFontTemplateDictionary, graphicsCompositorTemplateDictionary, visualScriptingTemplateDictionary,
                         visualScriptingGraphTemplatesDictionary,
                     })
            {
                RegisterResourceDictionary(context, dictionary);
                foreach (object entry in dictionary.Keys)
                {
                    if (entry is Type type)
                        typeImages[type] = dictionary[entry];
                }
            }
            foreach (var (type, image) in typeImages)
                context.TypeImages.Register(type, image);

            context.CopyProcessors.Register(new EntityComponentCopyProcessor());
            context.PasteProcessors.Register(new EntityComponentPasteProcessor());
            context.PasteProcessors.Register(new EntityHierarchyPasteProcessor());
            context.PostPasteProcessors.Register(new ScenePostPasteProcessor());

            EntityComponentMetadata.RegisterComponentOrders(context.Logger);
        }

        /// <inheritdoc />
        public override void InitializeSession(ISessionPluginContext context)
        {
            base.InitializeSession(context);

            var session = context.Session;
            var sessionViewModel = (SessionViewModel)session;
            context.Services.Register(new StrideDialogService());
            // The scene and prefab editors run their game in a WinForms form, and show the toolbar of the WPF editor
            context.Services.Register(WpfEditorGameViewport.Service);
            context.Services.Register(new WpfEditorViewOptionsFactory());
            var assetsViewModel = new StrideAssetsViewModel(sessionViewModel);

            context.NodePresenterCommands.Register(new FetchEntityCommand());
            context.NodePresenterCommands.Register(new SetEntityReferenceCommand());
            context.NodePresenterCommands.Register(new SetComponentReferenceCommand());
            context.NodePresenterCommands.Register(new SetSymbolReferenceCommand());
            context.NodePresenterCommands.Register(new PickupEntityCommand());
            context.NodePresenterCommands.Register(new PickupEntityComponentCommand());
            context.NodePresenterCommands.Register(new EditCurveCommand(sessionViewModel));
            context.NodePresenterCommands.Register(new SkeletonNodePreserveAllCommand());
            //TODO: Add back once properly implemented.
            //context.NodePresenterCommands.Register(new AddNewScriptComponentCommand());

            context.NodePresenterUpdaters.Register(new AnimationAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new CameraSlotNodeUpdater());
            context.NodePresenterUpdaters.Register(new EntityHierarchyAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new EntityHierarchyEditorNodeUpdater());
            context.NodePresenterUpdaters.Register(new GameSettingsAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new GraphicsCompositorAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new FXAAEffectNodeUpdater());
            context.NodePresenterUpdaters.Register(new MaterialAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new ModelAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new ModelNodeLinkNodeUpdater());
            context.NodePresenterUpdaters.Register(new SkeletonAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new SpriteFontAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new SpriteSheetAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new TextureAssetNodeUpdater());
            context.NodePresenterUpdaters.Register(new UnloadableObjectPropertyNodeUpdater());
            context.NodePresenterUpdaters.Register(new VisualScriptNodeUpdater());

            // Connects to effect compiler (to import new effect permutations discovered by running the game), until the
            // session closes
            if (Stride.Core.Assets.Editor.Settings.EditorSettings.UseEffectCompilerServer.GetValue())
            {
                var effectCompilerServerSession = new EffectCompilerServerSession(sessionViewModel);
                context.OnClose(effectCompilerServerSession.Dispose);
            }

            // Extra packages to display in "add reference" dialog
            sessionViewModel.SuggestedPackages.Add(new PackageName(typeof(Stride.Engine.EntityComponent).Assembly.GetName().Name, new PackageVersion(StrideVersion.NuGetVersion)));
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
        /// Gizmos, asset highlighters, add-asset policies and entity factories of <paramref name="assembly"/>, discovered
        /// through its source-generated scan index.
        /// </summary>
        private void OnRegisteredAssetAssembly(Assembly assembly)
        {
            GizmoTypeRegistry.RegisterAssembly(assembly);
            lock (TypeMapsLock)
            {
                assetHighlighterTypes = WithTypes(assetHighlighterTypes, GetAssetHighlighterTypes(assembly));
            }
            AddAssetPolicyRegistry.RegisterAssembly(assembly);

            var factoryTypes = AssemblyRegistry.GetScanTypes(assembly, typeof(IEntityFactory)).ToArray();
            OnUiThread(() => RegisterEntityFactories(factoryTypes));
        }

        private void OnUnregisteredAssetAssembly(Assembly assembly)
        {
            GizmoTypeRegistry.UnregisterAssembly(assembly);
            lock (TypeMapsLock)
            {
                assetHighlighterTypes = WithoutTypes(assetHighlighterTypes, GetAssetHighlighterTypes(assembly));
            }
            AddAssetPolicyRegistry.UnregisterAssembly(assembly);

            var factoryTypes = AssemblyRegistry.GetScanTypes(assembly, typeof(IEntityFactory)).ToArray();
            OnUiThread(() => UnregisterEntityFactories(factoryTypes));
        }

        // Asset type -> highlighter type
        private static List<KeyValuePair<Type, Type>> GetAssetHighlighterTypes(Assembly assembly)
        {
            var types = new List<KeyValuePair<Type, Type>>();
            foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(AssetHighlighterAttribute)))
            {
                if (!type.IsAssignableTo(typeof(AssetHighlighter)))
                    continue;
                foreach (var attribute in type.GetCustomAttributes<AssetHighlighterAttribute>(false).NotNull())
                    types.Add(KeyValuePair.Create(attribute.AssetType, type));
            }
            return types;
        }

        // The last registered type wins: two assemblies may declare one for the same key, and a reload can register the
        // new copy before the old one goes
        private static Dictionary<Type, Type> WithTypes(IReadOnlyDictionary<Type, Type> map, List<KeyValuePair<Type, Type>> types)
        {
            var result = new Dictionary<Type, Type>(map);
            foreach (var (key, type) in types)
                result[key] = type;
            return result;
        }

        // Only the types the assembly registered: a key another assembly took since keeps its type
        private static Dictionary<Type, Type> WithoutTypes(IReadOnlyDictionary<Type, Type> map, List<KeyValuePair<Type, Type>> types)
        {
            var result = new Dictionary<Type, Type>(map);
            foreach (var (key, type) in types)
            {
                if (result.TryGetValue(key, out var current) && current == type)
                    result.Remove(key);
            }
            return result;
        }

        private static bool IsEntityFactory(Type type)
        {
            return type.IsAssignableTo(typeof(IEntityFactory)) && type.IsClass && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null;
        }

        /// <summary>
        /// The entity factory lists are bound by the editor views, so they only change on the UI thread; an assembly
        /// registered from another thread (a package loaded later) queues its update.
        /// </summary>
        private static void OnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.InvokeAsync(action);
        }

        private static void RegisterEntityFactories(Type[] types)
        {
            foreach (var factoryType in types.Where(IsEntityFactory))
            {
                var display = factoryType.GetCustomAttribute<DisplayAttribute>();
                if (display == null)
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

                var instance = (IEntityFactory)Activator.CreateInstance(factoryType);
                // We use int.MaxValue / 2 to give enough space to all factories that do not have an Order value
                category.AddFactory(instance, display.Name, display.Order ?? int.MaxValue / 2);
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
}
