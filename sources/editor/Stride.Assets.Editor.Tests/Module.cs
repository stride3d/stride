// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Assets;

namespace Stride.Assets.Editor.Tests;

internal static class Module
{
    // Stride's attribute (not System.Runtime.CompilerServices'): the generated initializer runs the scan index registration first
    [Stride.Core.ModuleInitializer]
    internal static void Initialize()
    {
        // The asset types, their view models and the editor plugin: the agnostic editor layer registers everything below it
        RuntimeHelpers.RunModuleConstructor(typeof(Stride.Assets.Editor.Module).Module.ModuleHandle);

        // PackageSession needs MSBuild, like in the editor
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();
    }
}
