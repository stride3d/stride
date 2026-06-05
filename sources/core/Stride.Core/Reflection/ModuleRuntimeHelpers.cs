// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Stride.Core.Reflection;

public static class ModuleRuntimeHelpers
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "No-ModuleHandle fallback (e.g. Mono); module types kept by serializer registration.")]
    [UnconditionalSuppressMessage("Trimming", "IL2059", Justification = "No-ModuleHandle fallback (e.g. Mono); module types kept by serializer registration.")]
    public static void RunModuleConstructor(Module module)
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
