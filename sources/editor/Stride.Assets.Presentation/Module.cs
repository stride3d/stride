// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.Assets.Presentation;

internal static class Module
{
    [Core.ModuleInitializer]
    public static void Initialize()
    {
        // Make sure the engine asset assemblies are registered before this one, so they are scanned first
        RuntimeHelpers.RunModuleConstructor(typeof(MaterialAsset).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(SpriteFontAsset).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(Model).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(PrefabAsset).Module.ModuleHandle);
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
        // Each assembly translates its own strings: Tr._p looks the catalog up by the calling assembly
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(typeof(Module).Assembly));
        // This assembly holds no plugin: the cross-platform editor's StrideDefaultAssetsPlugin (Stride.Assets.Editor) covers
        // its view models, and the WPF editor registers the ones it shares itself
    }
}
