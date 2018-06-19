// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Reflection.Emit;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;
using Xenko.Engine;
using Xenko.Rendering;
using Xenko.Rendering.Skyboxes;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Assets
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Register solution platforms
            XenkoConfig.RegisterSolutionPlatforms();

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
