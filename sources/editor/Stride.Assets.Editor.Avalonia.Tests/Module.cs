// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Assets;

namespace Stride.Assets.Editor.Avalonia.Tests;

internal static class Module
{
    // Stride's attribute (not System.Runtime.CompilerServices'): the generated initializer runs the scan index registration first
    [Stride.Core.ModuleInitializer]
    internal static void Initialize()
    {
        // The view assemblies register their plugins and scan indexes when they initialize; the tests look them up by type
        RuntimeHelpers.RunModuleConstructor(typeof(StrideEditorViewPlugin).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(Stride.Audio.Editor.AudioEditorAvaloniaPlugin).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(Stride.SpriteStudio.Editor.SpriteStudioEditorAvaloniaPlugin).Module.ModuleHandle);

        // The template providers scan the loaded assemblies for types: the assets assemblies need MSBuild resolvable, like in the editor
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();
    }
}
