// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Views;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Reflection;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;
using Stride.Engine.Gizmos;
using Xunit;

namespace Stride.GameStudio.Tests
{
    /// <summary>
    /// The editor finds plugins, gizmos, previews, factories and services through the source-generated scan index;
    /// the index of the default plugin assembly must list the known ones.
    /// </summary>
    public class TestDiscoveryIndex
    {
        [Theory]
        [InlineData(typeof(AssetsPlugin), typeof(StrideDefaultAssetsPlugin))]
        [InlineData(typeof(IAddAssetPolicy), typeof(AddPrefabAssetPolicy))]
        [InlineData(typeof(AssetViewModel), typeof(SceneViewModel))]
        [InlineData(typeof(IPreviewView), typeof(ModelPreviewView))]
        [InlineData(typeof(IAssetEditorViewModel), typeof(SceneEditorViewModel))]
        [InlineData(typeof(IEditorView), typeof(SceneEditorView))]
        [InlineData(typeof(EditorGameServiceAttribute), typeof(EditorGameEntityTransformService))]
        public void DefaultPluginAssemblyIndexListsKnownTypes(Type key, Type expected)
        {
            var assembly = typeof(StrideDefaultAssetsPlugin).Assembly;
            Assert.NotNull(AssemblyRegistry.GetScanTypes(assembly));
            Assert.Contains(expected, AssemblyRegistry.GetScanTypes(assembly, key).ToList());
        }

        [Theory]
        [InlineData(typeof(GizmoComponentAttribute), typeof(DispatcherLightGizmo))]
        [InlineData(typeof(GizmoComponentAttribute), typeof(CameraGizmo))]
        [InlineData(typeof(AssetsPlugin), typeof(StrideSharedAssetsPlugin))]
        [InlineData(typeof(AssetViewModel), typeof(MaterialViewModel))]
        [InlineData(typeof(IEntityFactory), typeof(SpriteEntityFactory))]
        [InlineData(typeof(IAssetPreview), typeof(ModelPreview))]
        [InlineData(typeof(IAssetPreviewViewModel), typeof(ModelPreviewViewModel))]
        public void SharedAssemblyIndexListsKnownTypes(Type key, Type expected)
        {
            // The WPF-free gizmos live in Stride.Assets.Presentation.Common, which the WPF assembly registers when it loads
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsPlugin).Module.ModuleHandle);
            var assembly = typeof(GizmoBase).Assembly;
            Assert.Contains(assembly, AssemblyRegistry.Find(AssemblyCommonCategories.Assets));
            Assert.NotNull(AssemblyRegistry.GetScanTypes(assembly));
            Assert.Contains(expected, AssemblyRegistry.GetScanTypes(assembly, key).ToList());
        }

        [Fact]
        public void EachAssetTypeHasOneViewModelInTheWpfEditor()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsPlugin).Module.ModuleHandle);
            var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);

            // The shared view models come from Stride.Assets.Presentation.Common; the Avalonia assembly of the same namespace
            // (Stride.Assets.Presentation) must not be an assets assembly of the WPF editor
            Assert.Contains(typeof(MaterialViewModel).Assembly, assemblies);
            Assert.DoesNotContain(assemblies, x => x.GetName().Name == "Stride.Assets.Presentation");
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name == "Stride.Assets.Presentation");

            var duplicates = assemblies
                .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(AssetViewModel)))
                .Select(x => (x.GetCustomAttribute<AssetViewModelAttribute>()?.AssetType, ViewModel: x))
                .Where(x => x.AssetType is not null)
                .GroupBy(x => x.AssetType)
                .Where(x => x.Count() > 1)
                .Select(x => $"{x.Key.Name}: {string.Join(", ", x.Select(y => y.ViewModel.FullName))}")
                .ToList();
            Assert.Empty(duplicates);
        }

        [Fact]
        public void EachAssetTypeHasOnePreviewAndEachPreviewOneViewModelInTheWpfEditor()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsPlugin).Module.ModuleHandle);
            var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);

            // The shared previews and their view models come from Stride.Assets.Presentation.Common, the WPF preview views
            // from the WPF assembly
            Assert.Contains(typeof(ModelPreview).Assembly, assemblies);
            Assert.Equal(typeof(StrideSharedAssetsPlugin).Assembly, typeof(ModelPreview).Assembly);
            Assert.Equal(typeof(StrideSharedAssetsPlugin).Assembly, typeof(ModelPreviewViewModel).Assembly);
            Assert.True(typeof(StrideSharedAssetsPlugin).IsAssignableTo(typeof(StrideAssetsPlugin)), "The shared plugin must be a StrideAssetsPlugin, or its preview view models are not registered.");

            // The preview service registers one preview per asset type (the last one wins): two would hide one silently
            var previews = assemblies
                .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IAssetPreview)))
                .Select(x => (x.GetCustomAttribute<AssetPreviewAttribute>()?.AssetType, Preview: x))
                .Where(x => x.AssetType is not null)
                .ToList();
            Assert.Empty(Duplicates(previews, x => x.AssetType, x => x.Preview));

            // The plugin host registers one view model per preview type, from the assemblies of the Stride plugins
            var viewModels = assemblies
                .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IAssetPreviewViewModel)))
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
            RuntimeHelpers.RunModuleConstructor(typeof(StrideDefaultAssetsPlugin).Module.ModuleHandle);

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
