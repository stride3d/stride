// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Assets.Quantum;
using Stride.Core.Reflection;

namespace Stride.Assets.Presentation;

internal class CommonModule
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // The layers below register first, so that loading this assembly is enough to scan the whole shared stack
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Stride.Editor.Thumbnails.ThumbnailCompilerBase<>).Module.ModuleHandle);
        // Gizmos, thumbnail compilers, editor game services and paste processors are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(CommonModule).Assembly, AssemblyCommonCategories.Assets);
        AssetQuantumRegistry.RegisterAssembly(typeof(CommonModule).Assembly);
    }
}
