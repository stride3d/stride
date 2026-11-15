// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Reflection;
using Stride.Editor.Thumbnails;

namespace Stride.Editor.Tests;

internal static class Module
{
    // Stride's attribute (not System.Runtime.CompilerServices'): the generated initializer runs the scan index registration first,
    // which the asset registration below needs
    [Stride.Core.ModuleInitializer]
    internal static void Initialize()
    {
        // The settings files the sessions save, away from the user's own and apart for each test process
        EditorPath.UserDataPath = Path.Combine(Path.GetTempPath(), "Stride.Editor.Tests", "config-" + Environment.ProcessId);

        // The test asset types (and their static thumbnail) must be known to load the saved packages
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(GameSettingsAsset).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(SessionViewModel).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(GameStudioThumbnailService).Module.ModuleHandle);

        // PackageSession needs MSBuild, like in the editor
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();
    }
}
