// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Views;
using Stride.Assets.Presentation.Preview;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Presentation.ViewModel.Preview;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Reflection;
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
        [InlineData(typeof(IEntityFactory), typeof(SpriteEntityFactory))]
        [InlineData(typeof(IAddAssetPolicy), typeof(AddPrefabAssetPolicy))]
        [InlineData(typeof(AssetViewModel), typeof(SceneViewModel))]
        [InlineData(typeof(IAssetPreview), typeof(ModelPreview))]
        [InlineData(typeof(IPreviewView), typeof(ModelPreviewView))]
        [InlineData(typeof(IAssetPreviewViewModel), typeof(ModelPreviewViewModel))]
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
    }
}
