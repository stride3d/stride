// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Assets.Editor.Tests;

public class TestModules
{
    [Fact]
    public void BothAssembliesOfTheLayerRegisterAsAssetsAssemblies()
    {
        var presentation = typeof(AssetViewModel).Assembly;
        var editor = typeof(DisplayData).Assembly;
        Assert.NotEqual(presentation, editor);

        // Initializing the editor assembly initializes the presentation assembly first
        RuntimeHelpers.RunModuleConstructor(editor.ManifestModule.ModuleHandle);

        // Registered so that serialization and asset services see their types (the asset view models, the node updaters and the paste processors)
        var assets = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);
        Assert.Contains(presentation, assets);
        Assert.Contains(editor, assets);
    }

    [Fact]
    public void BothAssembliesOfTheLayerRegisterTheirTranslations()
    {
        var presentation = typeof(AssetViewModel).Assembly;
        var editor = typeof(DisplayData).Assembly;
        RuntimeHelpers.RunModuleConstructor(editor.ManifestModule.ModuleHandle);

        // Each assembly translates its own strings (Tr._p looks the provider up by the calling assembly):
        // the manager takes one provider per assembly, so a second one is refused
        Assert.Throws<ArgumentException>(() => TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(presentation)));
        Assert.Throws<ArgumentException>(() => TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(editor)));
    }
}
