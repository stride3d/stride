// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.UI.Editor;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // This assembly translates its own strings: Tr._p looks the catalog up by the calling assembly
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(typeof(Module).Assembly));
    }
}
