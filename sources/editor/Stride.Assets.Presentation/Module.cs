// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.Templates;
using Stride.Assets.SpriteFont;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.Assets.Presentation
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
            StrideTemplates.Register();
            // Initialize translation
            TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
        }
    }
}
