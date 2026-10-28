// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets.Presentation;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Reflection;

namespace Stride.Assets.Editor;

internal static class Module
{
    [Core.ModuleInitializer]
    public static void Initialize()
    {
        // Register the layers below this assembly first, so they are scanned first: the WPF-free shared part and the
        // toolkit-neutral asset view models (which register the engine asset assemblies)
        RuntimeHelpers.RunModuleConstructor(typeof(CommonModule).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(Presentation.Module).Module.ModuleHandle);
        // The previews and thumbnail compilers of this assembly are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        AssetsPlugin.RegisterPlugin(typeof(StrideEditorPlugin));
    }
}
