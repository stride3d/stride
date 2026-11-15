// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Assets.Editor;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Register the presentation layer before this assembly, so it is scanned first
        RuntimeHelpers.RunModuleConstructor(typeof(Stride.Core.Assets.Presentation.Module).Module.ModuleHandle);
        // Node updaters, node commands and paste processors are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        // Initialize translation
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
    }
}
