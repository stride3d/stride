// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Assets.Editor
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Register the WPF-free part of this layer before this assembly, so it is scanned first
            System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(CommonModule).Module.ModuleHandle);
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
            AssetsPlugin.RegisterPlugin(typeof(CoreAssetsEditorPlugin));
            // Initialize translation
            TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
            // HACK
            Presentation.Quantum.ViewModels.NodeViewModel.UnsetValue = System.Windows.DependencyProperty.UnsetValue;
        }
    }
}
