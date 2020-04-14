// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Reflection.Emit;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Skyboxes;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Assets
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Register solution platforms
            StrideConfig.RegisterSolutionPlatforms();

            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
            AssemblyRegistry.Register(typeof(ParameterKeys).Assembly, AssemblyCommonCategories.Assets);
            AssemblyRegistry.Register(typeof(Texture).Assembly, AssemblyCommonCategories.Assets);
            AssemblyRegistry.Register(typeof(ShaderClassSource).Assembly, AssemblyCommonCategories.Assets);

            // Add AllowMultipleComponentsAttribute on EntityComponent yaml proxy (since there might be more than one)
            UnloadableObjectInstantiator.ProcessProxyType += ProcessEntityComponent;
        }

        private static void ProcessEntityComponent(Type baseType, TypeBuilder typeBuilder)
        {
            if (typeof(EntityComponent).IsAssignableFrom(baseType))
            {
                // Add AllowMultipleComponentsAttribute on EntityComponent yaml proxy (since there might be more than one)
                var allowMultipleComponentsAttributeCtor = typeof(AllowMultipleComponentsAttribute).GetConstructor(Type.EmptyTypes);
                var allowMultipleComponentsAttribute = new CustomAttributeBuilder(allowMultipleComponentsAttributeCtor, new object[0]);
                typeBuilder.SetCustomAttribute(allowMultipleComponentsAttribute);
            }
        }
    }
}
