// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Assets.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;
using Stride.Editor.Thumbnails;

namespace Stride.Assets.Editor;

internal static class Module
{
    [Core.ModuleInitializer]
    public static void Initialize()
    {
        // Register the layers below this assembly first, so they are scanned first: the editor layer and the asset view models
        // (which register the engine asset assemblies)
        RuntimeHelpers.RunModuleConstructor(typeof(ThumbnailCompilerBase<>).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(Presentation.Module).Module.ModuleHandle);
        // Gizmos, previews, thumbnail compilers, editor game services and paste processors are discovered through the assets
        // assemblies
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
        // Each assembly translates its own strings: Tr._p looks the catalog up by the calling assembly
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(typeof(Module).Assembly));
        // The default plugin (StrideEditorPlugin) is registered by the cross-platform editor host, so that loading this
        // assembly in another editor does not add it
    }
}
