// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.Editor
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Register the WPF-free part of this layer before this assembly, so it is scanned first
            System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(CommonModule).Module.ModuleHandle);
            // Currently, we are adding this assembly as part of the "assets" in order for the thumbnail types to be accessible through the AssemblyRegistry
            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
