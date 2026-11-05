// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Assets.Presentation;

internal static class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // The asset view models are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        // Initialize translation
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
    }
}
