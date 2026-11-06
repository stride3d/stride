// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Views;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.Reflection;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;
using Stride.Engine.Gizmos;
using Xunit;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Preview.ViewModels;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.AssetEditors.Gizmos;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;

namespace Stride.GameStudio.Tests
{
    /// <summary>
    /// The editor finds plugins, gizmos, previews, factories and services through the source-generated scan index;
    /// the index of the default plugin assembly must list the known ones.
    /// </summary>
    public class TestDiscoveryIndex
    {
        [Theory]
        [InlineData(typeof(AssetsPlugin), typeof(StrideDefaultAssetsWpfPlugin))]
        [InlineData(typeof(AssetViewModel), typeof(CodeAssetViewModel))]
        [InlineData(typeof(IPreviewView), typeof(ModelPreviewView))]
        [InlineData(typeof(IEditorView), typeof(SceneEditorView))]
        [InlineData(typeof(EditorGameServiceAttribute), typeof(EditorGameRenderModeService))]
        public void DefaultPluginAssemblyIndexListsKnownTypes(Type key, Type expected)
        {
            var assembly = typeof(StrideDefaultAssetsWpfPlugin).Assembly;
            Assert.NotNull(AssemblyRegistry.GetScanTypes(assembly));
            Assert.Contains(expected, AssemblyRegistry.GetScanTypes(assembly, key).ToList());
        }

        [Theory]
        [InlineData(typeof(GizmoComponentAttribute), typeof(DispatcherLightGizmo))]
        [InlineData(typeof(GizmoComponentAttribute), typeof(CameraGizmo))]
        [InlineData(typeof(AssetsPlugin), typeof(Stride.Assets.Editor.StrideEditorPlugin))]
        [InlineData(typeof(AssetViewModel), typeof(MaterialViewModel))]
        [InlineData(typeof(AssetViewModel), typeof(SceneViewModel))]
        [InlineData(typeof(AssetViewModel), typeof(PrefabViewModel))]
        [InlineData(typeof(AssetViewModel), typeof(GameSettingsViewModel))]
        [InlineData(typeof(IEntityFactory), typeof(SpriteEntityFactory))]
        [InlineData(typeof(IAddAssetPolicy), typeof(Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddPrefabAssetPolicy))]
        [InlineData(typeof(IAssetPreview), typeof(ModelPreview))]
        [InlineData(typeof(IAssetPreviewViewModel), typeof(ModelPreviewViewModel))]
        public void SharedAssemblyIndexListsKnownTypes(Type key, Type expected)
        {
            // The toolkit-neutral types live in Stride.Assets.Editor and Stride.Assets.Presentation (the asset view models),
            // which the WPF assembly registers when it loads
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsWpfPlugin).Module.ModuleHandle);
            var assembly = expected.Assembly;
            Assert.Contains(assembly, new[] { typeof(GizmoBase).Assembly, typeof(MaterialViewModel).Assembly });
            Assert.Contains(assembly, AssemblyRegistry.Find(AssemblyCommonCategories.Assets));
            Assert.NotNull(AssemblyRegistry.GetScanTypes(assembly));
            Assert.Contains(expected, AssemblyRegistry.GetScanTypes(assembly, key).ToList());
        }

        [Fact]
        public void EachAssetTypeHasOneViewModelInTheWpfEditor()
        {
            // The shared view models come from Stride.Assets.Presentation, an assets assembly of the WPF editor too; the view models
            // it declares for the cross-platform editor only (the script source file) are not registered
            var declared = WpfRegisteredTypes(typeof(AssetViewModel));
            Assert.Contains(typeof(MaterialViewModel).Assembly, AssemblyRegistry.Find(AssemblyCommonCategories.Assets));
            Assert.Contains(typeof(MaterialViewModel), declared);
            Assert.DoesNotContain(typeof(Stride.Assets.Presentation.ViewModels.ScriptSourceFileViewModel), declared);

            var duplicates = declared
                .Select(x => (x.GetCustomAttribute<AssetViewModelAttribute>()?.AssetType, ViewModel: x))
                .Where(x => x.AssetType is not null)
                .GroupBy(x => x.AssetType)
                .Where(x => x.Count() > 1)
                .Select(x => $"{x.Key.Name}: {string.Join(", ", x.Select(y => y.ViewModel.FullName))}")
                .ToList();
            Assert.Empty(duplicates);
        }

        [Fact]
        public void TheScenesPrefabsAndGameSettingsHaveTheSharedViewModelsInTheWpfEditor()
        {
            // The shared view models of the entity hierarchies (Stride.Assets.Presentation) replaced the WPF ones
            var viewModels = WpfRegisteredTypes(typeof(AssetViewModel))
                .Select(x => (x.GetCustomAttribute<AssetViewModelAttribute>()?.AssetType, ViewModel: x))
                .Where(x => x.AssetType is not null)
                .Distinct()
                .ToList();

            Assert.Equal(new[] { typeof(SceneViewModel) }, viewModels.Where(x => x.AssetType == typeof(Stride.Assets.Entities.SceneAsset)).Select(x => x.ViewModel));
            Assert.Equal(new[] { typeof(PrefabViewModel) }, viewModels.Where(x => x.AssetType == typeof(Stride.Assets.Entities.PrefabAsset)).Select(x => x.ViewModel));
            Assert.Equal(new[] { typeof(GameSettingsViewModel) }, viewModels.Where(x => x.AssetType == typeof(Stride.Assets.GameSettingsAsset)).Select(x => x.ViewModel));
            Assert.All(new[] { typeof(SceneViewModel), typeof(PrefabViewModel), typeof(GameSettingsViewModel), typeof(EntityViewModel) },
                x => Assert.Equal(typeof(MaterialViewModel).Assembly, x.Assembly));
            // So do their editors (Stride.Assets.Editor)
            Assert.Equal(typeof(GizmoBase).Assembly, typeof(Stride.Assets.Editor.ViewModels.SceneEditorViewModel).Assembly);
            Assert.Equal(typeof(GizmoBase).Assembly, typeof(Stride.Assets.Editor.ViewModels.PrefabEditorViewModel).Assembly);
        }

        [Fact]
        public void EachAssetTypeHasOnePreviewAndEachPreviewOneViewModelInTheWpfEditor()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsWpfPlugin).Module.ModuleHandle);
            var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);

            // The shared previews and their view models come from Stride.Assets.Editor, the WPF preview views from the WPF
            // assembly; the WPF default assets plugin registers the preview view models
            Assert.Contains(typeof(ModelPreview).Assembly, assemblies);
            Assert.Equal(typeof(GizmoBase).Assembly, typeof(ModelPreview).Assembly);
            Assert.Equal(typeof(GizmoBase).Assembly, typeof(ModelPreviewViewModel).Assembly);
            Assert.Contains(typeof(ModelPreviewViewModel), StrideDefaultAssetsWpfPlugin.SharedViewModelTypes);

            // The preview service registers one preview per asset type (the last one wins): two would hide one silently
            var previews = assemblies
                .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IAssetPreview)))
                .Select(x => (x.GetCustomAttribute<AssetPreviewAttribute>()?.AssetType, Preview: x))
                .Where(x => x.AssetType is not null)
                .ToList();
            Assert.Empty(Duplicates(previews, x => x.AssetType, x => x.Preview));

            // The plugin host registers one view model per preview type
            var viewModels = WpfRegisteredTypes(typeof(IAssetPreviewViewModel))
                .Select(x => (x.GetCustomAttribute<AssetPreviewViewModelAttribute>()?.AssetPreviewType, ViewModel: x))
                .Where(x => x.AssetPreviewType is not null)
                .ToList();
            Assert.Empty(Duplicates(viewModels, x => x.AssetPreviewType, x => x.ViewModel));

            // Each preview of the shared assembly has a view model, its own or one of a base type (sprite batch previews)
            foreach (var (_, preview) in previews.Where(x => x.Preview.Assembly == typeof(ModelPreview).Assembly))
            {
                var hasViewModel = false;
                for (var type = preview; type is not null && !hasViewModel; type = type.BaseType)
                {
                    var key = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    hasViewModel = viewModels.Any(x => x.AssetPreviewType == key);
                }
                // The scene and prefab previews have no view model, as before the move
                Assert.True(hasViewModel || preview == typeof(ScenePreview) || preview == typeof(PrefabPreview) || preview == typeof(PrefabModelPreview), $"{preview.Name} has no view model.");
            }
        }

        [Fact]
        public void EachEntityFactoryIsRegisteredOnceInTheWpfEditor()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsWpfPlugin).Module.ModuleHandle);

            // The add-entity menus list the factories of the assets assemblies, by display category and name. The plugin
            // fills them when the editor initializes it, which takes the WPF application, so this reads the same types from
            // the scan index, as the plugin does.
            RuntimeHelpers.RunModuleConstructor(typeof(SpriteEntityFactory).Module.ModuleHandle);
            var factories = Stride.Core.Assets.AssetRegistry.AssetAssemblies
                .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IEntityFactory))).Distinct()
                .Where(x => x.IsAssignableTo(typeof(IEntityFactory)) && x.IsClass && !x.IsAbstract && x.GetConstructor(Type.EmptyTypes) != null)
                .Select(x => (Type: x, Display: x.GetCustomAttribute<Stride.Core.DisplayAttribute>()))
                .Where(x => x.Display != null)
                .Select(x => (Entry: $"{x.Display.Category}/{x.Display.Name}", x.Type))
                .ToList();
            Assert.Contains(factories, x => x.Type == typeof(SpriteEntityFactory));
            Assert.Contains(factories, x => x.Type == typeof(ModelEntityFactory));
            Assert.All(factories.Where(x => x.Type.Namespace == typeof(SpriteEntityFactory).Namespace), x => Assert.Equal(typeof(SpriteEntityFactory).Assembly, x.Type.Assembly));
            Assert.Empty(Duplicates(factories, x => x.Type, x => x.Type));
            Assert.Empty(Duplicates(factories, x => x.Entry, x => x.Type));
        }

        [Fact]
        public void EachAssetTemplateHasOneGeneratorInTheWpfEditor()
        {
            // The module of the WPF assets layer registers the shared generators (Stride.Assets.Editor) with its dialogs
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsWpfPlugin).Module.ModuleHandle);

            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            var registered = (System.Collections.Generic.List<ITemplateGenerator>)typeof(TemplateManager).GetField("Generators", flags).GetValue(null);
            System.Collections.Generic.List<ITemplateGenerator> generators;
            lock (typeof(TemplateManager).GetField("ThisLock", flags).GetValue(null))
            {
                generators = registered.ToList();
            }
            var shared = generators.Where(x => x.GetType().Namespace == typeof(AssetTemplateGenerator).Namespace).ToList();
            Assert.All(shared, x => Assert.Equal(typeof(GizmoBase).Assembly, x.GetType().Assembly));
            Assert.Empty(Duplicates(shared, x => x.GetType(), x => x.GetType()));
            Assert.Equal(10, shared.Count);
            // No WPF copy is left
            Assert.DoesNotContain(generators, x => x.GetType().Namespace == "Stride.Assets.Presentation.Templates" && x is AssetTemplateGenerator);

            // Each kind of asset template resolves to its own generator, the specific ones before the generic ones
            foreach (var (description, expected) in AssetTemplateCases())
            {
                var generator = TemplateManager.FindTemplateGenerator<AssetTemplateGeneratorParameters>(description);
                Assert.True(expected == generator?.GetType(), $"{description.Name}: {generator?.GetType().FullName ?? "no generator"} instead of {expected.Name}");
            }
        }

        [Fact]
        public void EachEditorGameServiceIsDeclaredOnceInTheWpfEditor()
        {
            // A controller creates the services the assets assemblies declare. The shared ones (Stride.Assets.Editor)
            // replaced their WPF copies: no service is created twice under one name
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsWpfPlugin).Module.ModuleHandle);
            var services = AssetRegistry.AssetAssemblies.SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(EditorGameServiceAttribute))).Distinct().ToList();
            Assert.Empty(Duplicates(services, x => x.Name, x => x));
            Assert.Contains(typeof(EditorGameCameraOrientationService), services);
            Assert.Equal(typeof(GizmoBase).Assembly, typeof(EditorGameCameraOrientationService).Assembly);

            // The shared view cube serves the WPF scene and prefab editors, through the shared controller interface
            var declaration = typeof(EditorGameCameraOrientationService).GetCustomAttribute<EditorGameServiceAttribute>();
            Assert.True(declaration.ControllerType.IsAssignableFrom(typeof(EntityHierarchyEditorController)));
            // The scene and prefab controllers are the shared ones
            Assert.Equal(typeof(GizmoBase).Assembly, typeof(EntityHierarchyEditorController).Assembly);
            Assert.True(typeof(EditorGameController<EntityHierarchyEditorGame>).IsAssignableFrom(typeof(EntityHierarchyEditorController)));
            // So are the grid, graphics compositor, gizmo, selection, material highlight, camera, light probe, wireframe stage
            // and tool services
            foreach (var serviceType in new[]
            {
                typeof(EditorGameViewportGridService), typeof(EditorGameGraphicsCompositorService), typeof(EditorGameComponentGizmoService),
                typeof(EditorGameEntitySelectionService), typeof(EditorGameModelSelectionService), typeof(EditorGameMaterialHighlightService),
                typeof(EditorGameEntityCameraService), typeof(EditorGameLightProbeGizmoService), typeof(WireframeGizmoStageService),
                typeof(Stride.Assets.Editor.EditorGame.Tools.EditorGameToolService),
            })
            {
                Assert.Contains(serviceType, services);
                Assert.Equal(typeof(GizmoBase).Assembly, serviceType.Assembly);
                Assert.True(serviceType.GetCustomAttribute<EditorGameServiceAttribute>().ControllerType.IsAssignableFrom(typeof(EntityHierarchyEditorController)), serviceType.Name);
            }
            // The light probes have the shared gizmo, and only it. This reads the scan index, as the gizmo registry does: the
            // registry itself is filled by the plugin when the editor initializes it, which takes the WPF application
            var lightProbeGizmos = AssetRegistry.AssetAssemblies.SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(GizmoComponentAttribute))).Distinct()
                .Where(x => x.GetCustomAttribute<GizmoComponentAttribute>(true)?.ComponentType == typeof(Stride.Engine.LightProbeComponent)).ToList();
            Assert.Equal(new[] { typeof(Stride.Assets.Editor.Gizmos.LightProbeGizmo) }, lightProbeGizmos);
            // The prefab light service is shared too, and serves the prefab editor only
            Assert.Contains(typeof(PrefabEditorLightService), services);
            Assert.Equal(typeof(EntityHierarchyEditorController).Assembly, typeof(PrefabEditorLightService).Assembly);
            Assert.True(typeof(PrefabEditorLightService).GetCustomAttribute<EditorGameServiceAttribute>().ControllerType.IsAssignableFrom(typeof(PrefabEditorController)));
            Assert.False(typeof(PrefabEditorLightService).GetCustomAttribute<EditorGameServiceAttribute>().ControllerType.IsAssignableFrom(typeof(SceneEditorController)));

            // The WPF hosting of the game is a WPF service, for the game of every editor (scenes, prefabs and UI)
            Assert.Contains(typeof(WpfEditorGameHostService), services);
            Assert.Equal(typeof(StrideDefaultAssetsWpfPlugin).Assembly, typeof(WpfEditorGameHostService).Assembly);
            Assert.Equal(typeof(IEditorGameController), typeof(WpfEditorGameHostService).GetCustomAttribute<EditorGameServiceAttribute>().ControllerType);
            // The pointer position over the game is shared: one service for the game of every editor, which reads the WPF viewport
            Assert.Equal(new[] { typeof(EditorGameMousePositionService) }, services.Where(x => typeof(IEditorGameMousePositionViewModelService).IsAssignableFrom(x)));
            Assert.Equal(typeof(GizmoBase).Assembly, typeof(EditorGameMousePositionService).Assembly);
            Assert.Equal(typeof(IEditorGameController), typeof(EditorGameMousePositionService).GetCustomAttribute<EditorGameServiceAttribute>().ControllerType);
        }

        [Theory]
        [InlineData(typeof(SpriteSheetViewModel), typeof(Stride.Assets.Editor.ViewModels.SpriteSheetEditorViewModel), typeof(Stride.Assets.Presentation.AssetEditors.SpriteEditor.Views.SpriteEditorView))]
        [InlineData(typeof(GraphicsCompositorViewModel), typeof(Stride.Assets.Editor.ViewModels.GraphicsCompositorEditorViewModel), typeof(Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.Views.GraphicsCompositorEditorView))]
        public void TheSharedEditorsOpenTheirAssetInOneEditorInTheWpfEditor(Type assetViewModelType, Type editorType, Type viewType)
        {
            // The shared editors (Stride.Assets.Editor) replaced the WPF ones: one editor per asset, one WPF view per
            // editor
            var (editors, views) = CollectEditorsAndViews();

            Assert.Equal(new[] { editorType }, editors.Where(x => x.ViewModelType == assetViewModelType).Select(x => x.Editor));
            Assert.Equal(typeof(GizmoBase).Assembly, editorType.Assembly);
            Assert.Equal(new[] { viewType }, views.Where(x => x.EditorViewModelType == editorType).Select(x => x.View));
        }

        [Fact]
        public void TheScenesAndPrefabsOpenInOneEditorInTheWpfEditor()
        {
            // The shared scene and prefab editors (Stride.Assets.Editor) replaced the WPF ones: one editor per
            // asset, shown by the WPF views
            var (editors, views) = CollectEditorsAndViews();

            var sceneEditor = typeof(Stride.Assets.Editor.ViewModels.SceneEditorViewModel);
            var prefabEditor = typeof(Stride.Assets.Editor.ViewModels.PrefabEditorViewModel);
            Assert.Equal(new[] { sceneEditor }, editors.Where(x => x.ViewModelType == typeof(SceneViewModel)).Select(x => x.Editor));
            Assert.Equal(new[] { prefabEditor }, editors.Where(x => x.ViewModelType == typeof(PrefabViewModel)).Select(x => x.Editor));
            Assert.Equal(new[] { typeof(SceneEditorView) }, views.Where(x => x.EditorViewModelType == sceneEditor).Select(x => x.View));
            Assert.Equal(new[] { typeof(Stride.Assets.Presentation.AssetEditors.PrefabEditor.Views.PrefabEditorView) }, views.Where(x => x.EditorViewModelType == prefabEditor).Select(x => x.View));
            // The WPF view options come from the factory the WPF plugin registers
            Assert.True(typeof(Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels.WpfEditorViewOptionsFactory).IsAssignableTo(typeof(Stride.Assets.Editor.ViewModels.IEditorViewOptionsFactory)));
        }

        private static (System.Collections.Generic.List<(Type ViewModelType, Type Editor)> Editors, System.Collections.Generic.List<(Type EditorViewModelType, Type View)> Views) CollectEditorsAndViews()
        {
            // The plugin host registers the editors and editor views of the Stride plugins' assemblies, and the WPF default assets
            // plugin the shared editors
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsWpfPlugin).Module.ModuleHandle);
            var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);
            var editors = WpfRegisteredTypes(typeof(IAssetEditorViewModel))
                .Select(x => (x.GetCustomAttribute<AssetEditorViewModelAttribute>()?.ViewModelType, Editor: x))
                .Where(x => x.ViewModelType is not null)
                .Distinct()
                .ToList();
            Assert.Empty(Duplicates(editors, x => x.ViewModelType, x => x.Editor));
            var views = assemblies
                .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IEditorView)))
                .Select(x => (x.GetCustomAttribute<AssetEditorViewAttribute>()?.EditorViewModelType, View: x))
                .Where(x => x.EditorViewModelType is not null)
                .Distinct()
                .ToList();
            Assert.Empty(Duplicates(views, x => x.EditorViewModelType, x => x.View));
            return (editors, views);
        }

        [Fact]
        public void TheVisualScriptHasOneAssetViewModelTheSharedOneInTheWpfEditor()
        {
            // The shared asset view model (Stride.Assets.Presentation) replaced the WPF one; the WPF editor stays, and the
            // cross-platform one is not registered
            var viewModels = WpfRegisteredTypes(typeof(Stride.Core.Assets.Presentation.ViewModels.AssetViewModel))
                .Select(x => (x.GetCustomAttribute<Stride.Core.Assets.Presentation.Annotations.AssetViewModelAttribute>()?.AssetType, ViewModel: x))
                .Where(x => x.AssetType is not null)
                .Distinct()
                .ToList();
            Assert.Empty(Duplicates(viewModels, x => x.AssetType, x => x.ViewModel));
            Assert.Equal(new[] { typeof(Stride.Assets.Presentation.ViewModels.VisualScriptViewModel) }, viewModels.Where(x => x.AssetType == typeof(Stride.Assets.Scripts.VisualScriptAsset)).Select(x => x.ViewModel));
            Assert.Equal(typeof(MaterialViewModel).Assembly, typeof(Stride.Assets.Presentation.ViewModels.VisualScriptViewModel).Assembly);

            var editors = WpfRegisteredTypes(typeof(IAssetEditorViewModel))
                .Where(x => x.GetCustomAttribute<AssetEditorViewModelAttribute>()?.ViewModelType == typeof(Stride.Assets.Presentation.ViewModels.VisualScriptViewModel))
                .Distinct();
            Assert.Equal(new[] { typeof(Stride.Assets.Presentation.AssetEditors.VisualScriptEditor.VisualScriptEditorViewModel) }, editors);
        }

        private static System.Collections.Generic.IEnumerable<(TemplateDescription Description, Type Generator)> AssetTemplateCases()
        {
            static TemplateAssetFactoryDescription Factory(string name, Guid id, string assetTypeName, bool importSource = false)
                => new() { Id = id, Name = name, Scope = TemplateScope.Asset, AssetTypeName = assetTypeName, ImportSource = importSource };

            yield return (Factory("Material", Guid.NewGuid(), nameof(Stride.Assets.Materials.MaterialAsset)), typeof(AssetFactoryTemplateGenerator));
            yield return (Factory("Texture", Guid.NewGuid(), nameof(Stride.Assets.Textures.TextureAsset), importSource: true), typeof(AssetFromFileTemplateGenerator));
            var derived = Factory("Graphics compositor", Guid.NewGuid(), nameof(Stride.Assets.Rendering.GraphicsCompositorAsset));
            derived.DerivedFrom = "GraphicsCompositor";
            yield return (derived, typeof(DerivedAssetTemplateGenerator));
            yield return (Factory("Procedural model", ProceduralModelFactoryTemplateGenerator.TemplateId, nameof(Stride.Assets.Models.ProceduralModelAsset)), typeof(ProceduralModelFactoryTemplateGenerator));
            yield return (Factory("Skybox", SkyboxFactoryTemplateGenerator.TemplateId, nameof(Stride.Assets.Skyboxes.SkyboxAsset)), typeof(SkyboxFactoryTemplateGenerator));
            yield return (Factory("Model", ModelFromFileTemplateGenerator.Id, nameof(Stride.Assets.Models.ModelAsset), importSource: true), typeof(ModelFromFileTemplateGenerator));
            yield return (Factory("Skeleton", SkeletonFromFileTemplateGenerator.Id, nameof(Stride.Assets.Models.SkeletonAsset), importSource: true), typeof(SkeletonFromFileTemplateGenerator));
            yield return (Factory("Animation", AnimationFromFileTemplateGenerator.Id, nameof(Stride.Assets.Models.AnimationAsset), importSource: true), typeof(AnimationFromFileTemplateGenerator));
            yield return (Factory("Sprite sheet", SpriteSheetFromFileTemplateGenerator.SpriteSheetId, nameof(Stride.Assets.Sprite.SpriteSheetAsset), importSource: true), typeof(SpriteSheetFromFileTemplateGenerator));
            yield return (Factory("UI sprite sheet", SpriteSheetFromFileTemplateGenerator.UISpriteSheetId, nameof(Stride.Assets.Sprite.SpriteSheetAsset), importSource: true), typeof(SpriteSheetFromFileTemplateGenerator));
            yield return (new TemplateAssetDescription { Id = Guid.NewGuid(), Name = "Script", Scope = TemplateScope.Asset, AssetTypeName = "ScriptSourceFileAsset" }, typeof(ScriptTemplateGenerator));
        }

        /// <summary>
        /// The types of <paramref name="baseType"/> that the WPF plugins register: the ones the assets assemblies declare, except
        /// the agnostic Stride.Assets.Presentation and Stride.Assets.Editor (whose plugins are the default plugins of the
        /// cross-platform editor), and the ones the WPF default assets plugin shares with that editor.
        /// </summary>
        private static System.Collections.Generic.List<Type> WpfRegisteredTypes(Type baseType)
        {
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsWpfPlugin).Module.ModuleHandle);
            var agnostic = new[] { typeof(MaterialViewModel).Assembly, typeof(GizmoBase).Assembly };
            return AssemblyRegistry.Find(AssemblyCommonCategories.Assets)
                .Where(x => !agnostic.Contains(x))
                .SelectMany(x => AssemblyRegistry.GetScanTypes(x, baseType))
                .Concat(StrideDefaultAssetsWpfPlugin.SharedViewModelTypes.Where(baseType.IsAssignableFrom))
                .Distinct()
                .ToList();
        }

        private static string[] Duplicates<T, TKey>(System.Collections.Generic.IEnumerable<T> items, Func<T, TKey> key, Func<T, Type> type)
        {
            return items
                .GroupBy(key)
                .Where(x => x.Count() > 1)
                .Select(x => $"{x.Key}: {string.Join(", ", x.Select(y => type(y).FullName))}")
                .ToArray();
        }
    }
}
