// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.Engine
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);

            // Preload proper libbulletc native library (depending on CPU type). A host that only reads physics assets
            // can run without it, and a failing module initializer would leave the assembly unusable: the physics
            // system preloads it again, and reports it missing, when a game uses it.
            try
            {
                NativeLibraryHelper.PreloadLibrary("libbulletc", typeof(PhysicsComponent));
            }
            catch (DllNotFoundException)
            {
            }
        }
    }
}
