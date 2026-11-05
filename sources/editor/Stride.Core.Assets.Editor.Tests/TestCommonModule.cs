// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Common.Tests;

public class TestCommonModule
{
    [Fact]
    public void AssemblyRegistersAsAssetsAssembly()
    {
        var assembly = typeof(DisplayData).Assembly;
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);

        // Registered so that serialization and asset services see its types; it declares no scanned types of its own
        Assert.Contains(assembly, AssemblyRegistry.Find(AssemblyCommonCategories.Assets));
    }
}
