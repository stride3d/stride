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
        // Register the WPF-free part of this layer before this assembly, so it is scanned first
        RuntimeHelpers.RunModuleConstructor(typeof(CommonModule).Module.ModuleHandle);
        // The previews and thumbnail compilers of this assembly are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        AssetsPlugin.RegisterPlugin(typeof(StrideEditorPlugin));
    }
}
