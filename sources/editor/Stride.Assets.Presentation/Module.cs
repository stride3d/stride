// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Xenko.Core.Assets.Quantum;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Translation;
using Xenko.Core.Translation.Providers;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.Templates;
using Xenko.Assets.SpriteFont;
using Xenko.Rendering;
using Xenko.Rendering.Materials;

namespace Xenko.Assets.Presentation
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(SpriteFontAsset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(Model).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(PrefabAsset).Module.ModuleHandle);
            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
            // We need access to the AssetQuantumRegistry from the SessionTemplateGenerator so for now we register graph types in the module initializer.
            AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
            // Register default template
            XenkoTemplates.Register();
            // Initialize translation
            TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
        }
    }
}
