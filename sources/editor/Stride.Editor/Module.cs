// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.Editor;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Register the shared part of this layer before this assembly, so it is scanned first
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(CommonModule).Module.ModuleHandle);
        // This assembly is part of the "assets" so that its thumbnail and preview types are reachable through the AssemblyRegistry
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
    }
}
