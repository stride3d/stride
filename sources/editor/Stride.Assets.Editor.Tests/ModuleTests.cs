// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets.Presentation;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;
using Xunit;

namespace Stride.Assets.Editor.Tests;

public sealed class ModuleTests
{
    [Fact]
    public void LoadingTheAssemblyRegistersItAndTheLayersBelowAsAssetsAssemblies()
    {
        // The assembly's one module (it also holds the former shared assets editor layer) registers the editor layer and the
        // asset view models, then itself
        var assets = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);

        Assert.Contains(typeof(Stride.Editor.Thumbnails.ThumbnailCompilerBase<>).Assembly, assets);
        Assert.Contains(typeof(Stride.Assets.Presentation.ViewModels.MaterialViewModel).Assembly, assets);
        Assert.Contains(typeof(StrideEditorPlugin).Assembly, assets);
    }

    [Fact]
    public void EachSharedAssetsEditorAssemblyRegistersItsTranslations()
    {
        // Tr._p looks the catalog up by the calling assembly, so an assembly without its own provider shows its texts in English
        // even when the WPF editor runs in another language
        var assemblies = new[]
        {
            typeof(Stride.Editor.Thumbnails.ThumbnailCompilerBase<>).Assembly,
            typeof(Stride.Assets.Presentation.ViewModels.MaterialViewModel).Assembly,
            typeof(StrideEditorPlugin).Assembly,
        };
        RuntimeHelpers.RunModuleConstructor(typeof(StrideEditorPlugin).Module.ModuleHandle);

        // The manager takes one provider per assembly, so a second one is refused
        foreach (var assembly in assemblies)
        {
            Assert.Throws<ArgumentException>(() => TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(assembly)));
        }
    }
}
