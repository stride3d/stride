// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets.Presentation;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Assets.Editor.Tests;

internal static class Module
{
    // Stride's attribute (not System.Runtime.CompilerServices'): the generated initializer runs the scan index registration first
    [Stride.Core.ModuleInitializer]
    internal static void Initialize()
    {
        // The settings files the sessions save, away from the user's own and apart for each test process
        EditorPath.UserDataPath = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", "config-" + Environment.ProcessId);

        // The asset types and their view models: the agnostic editor layer registers everything below it
        RuntimeHelpers.RunModuleConstructor(typeof(Stride.Assets.Editor.Module).Module.ModuleHandle);
        // The default plugins of the cross-platform editor, which its host registers (Stride.Assets.Editor.Avalonia)
        AssetsPlugin.RegisterPlugin(typeof(StrideDefaultAssetsPlugin));
        AssetsPlugin.RegisterPlugin(typeof(StrideEditorPlugin));

        // PackageSession needs MSBuild, like in the editor
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();
    }
}
