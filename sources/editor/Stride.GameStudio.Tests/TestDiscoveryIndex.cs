// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Views;
using Stride.Assets.Presentation.Preview;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Presentation.ViewModel.Preview;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Reflection;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;
using Xunit;

namespace Stride.GameStudio.Tests
{
    /// <summary>
    /// The editor finds plugins, previews, editors and views through the assembly processor's scan index instead of
    /// scanning types; the index of the default plugin assembly must list the known ones.
    /// </summary>
    public class TestDiscoveryIndex
    {
        [Theory]
        [InlineData(typeof(AssetsPlugin), typeof(StrideDefaultAssetsPlugin))]
        [InlineData(typeof(AssetViewModel), typeof(SceneViewModel))]
        [InlineData(typeof(IAssetPreview), typeof(ModelPreview))]
        [InlineData(typeof(IPreviewView), typeof(ModelPreviewView))]
        [InlineData(typeof(IAssetPreviewViewModel), typeof(ModelPreviewViewModel))]
        [InlineData(typeof(IAssetEditorViewModel), typeof(SceneEditorViewModel))]
        [InlineData(typeof(IEditorView), typeof(SceneEditorView))]
        public void DefaultPluginAssemblyIndexListsKnownTypes(Type key, Type expected)
        {
            var assembly = typeof(StrideDefaultAssetsPlugin).Assembly;
            Assert.NotNull(AssemblyRegistry.GetScanTypes(assembly));
            Assert.Contains(expected, AssemblyRegistry.GetScanTypes(assembly, key).ToList());
        }
    }
}
