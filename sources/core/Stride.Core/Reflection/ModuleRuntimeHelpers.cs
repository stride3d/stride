// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Core.Reflection
{
    public static class ModuleRuntimeHelpers
    {
        public static void RunModuleConstructor([NotNull] Module module)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
        }
    }
}
