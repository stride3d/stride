// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.Editor;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // The layer below registers first, so that loading this assembly is enough to scan the whole shared stack
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(AssetsPlugin).Module.ModuleHandle);
        // Thumbnail compilers, previews and editor game services are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        // Each assembly translates its own strings: Tr._p looks the catalog up by the calling assembly
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(typeof(Module).Assembly));
    }
}
