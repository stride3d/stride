// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Core.Reflection
{
    public static class ModuleRuntimeHelpers
    {
        public static void RunModuleConstructor([NotNull] Module module)
        {
            // On some platforms such as Android, ModuleHandle is not set
            if (module.ModuleHandle == ModuleHandle.EmptyHandle)
            {
                // Instead, initialize any type
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(module.Assembly.DefinedTypes.First().TypeHandle);
            }
            else
            {
                System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
            }
        }
    }
}
