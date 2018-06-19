// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xenko.Core.Annotations;

namespace Xenko.Core.Reflection
{
    public static class ModuleRuntimeHelpers
    {
        public static void RunModuleConstructor([NotNull] Module module)
        {
#if XENKO_PLATFORM_UWP || XENKO_RUNTIME_CORECLR
            // Initialize first type
            // TODO: Find a type without actual .cctor if possible, to avoid side effects
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(module.Assembly.DefinedTypes.First().AsType().TypeHandle);
#else
            System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
#endif
        }
    }
}
