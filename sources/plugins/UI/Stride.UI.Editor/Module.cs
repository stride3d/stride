// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.UI.Editor;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // This assembly translates its own strings: Tr._p looks the catalog up by the calling assembly. A Game Studio loads it from
        // memory, so its catalogs are found from the file its container loaded it from
        var assembly = typeof(Module).Assembly;
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(assembly, AssemblyContainer.GetLoadedPath(assembly)));
    }
}
