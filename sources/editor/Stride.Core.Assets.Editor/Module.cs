// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor;

internal class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Register the shared part of this layer before this assembly, so it is scanned first (it also registers the translation provider)
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(CommonModule).Module.ModuleHandle);
        AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
    }
}
