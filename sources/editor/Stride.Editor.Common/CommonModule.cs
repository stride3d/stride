// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.Editor;

internal class CommonModule
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // The layer below registers first, so that loading this assembly is enough to scan the whole shared stack
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys.DisplayData).Module.ModuleHandle);
        // Thumbnail compilers and editor game services are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(CommonModule).Assembly, AssemblyCommonCategories.Assets);
    }
}
