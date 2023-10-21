// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets.Materials;
using Stride.Core.Assets.Quantum;
using Stride.Core.Reflection;

namespace Stride.Assets.Presentation;

internal class Module
{
    [Core.ModuleInitializer]
    public static void Initialize()
    {
        RuntimeHelpers.RunModuleConstructor(typeof(MaterialAsset).Module.ModuleHandle);
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
    }
}
