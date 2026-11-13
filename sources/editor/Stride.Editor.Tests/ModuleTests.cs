// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.Editor.Build;

namespace Stride.Editor.Tests;

public sealed class ModuleTests
{
    [Fact]
    public void LoadingTheEditorLayerRegistersItAndTheLayersBelowAsAssetsAssemblies()
    {
        var editor = typeof(GameStudioDatabase).Assembly;
        RuntimeHelpers.RunModuleConstructor(editor.ManifestModule.ModuleHandle);

        // Its thumbnail compilers, previews and editor game services are discovered through the assets assemblies,
        // and initializing it initializes the core editor layers first
        var assets = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);
        Assert.Contains(editor, assets);
        Assert.Contains(typeof(AssetsPlugin).Assembly, assets);
        Assert.Contains(typeof(AssetViewModel).Assembly, assets);
    }
}
