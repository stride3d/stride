// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Assets.Editor;

internal class CommonModule
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Node updaters, node commands and paste processors are discovered through the assets assemblies
        AssemblyRegistry.Register(typeof(CommonModule).Assembly, AssemblyCommonCategories.Assets);
        // Initialize translation
        TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
    }
}
