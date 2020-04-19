// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.SpriteStudio.Offline
{
    public static class Module
    {
        [ModuleInitializer]
        public static void InitializeModule()
        {
            //RegisterPlugin(typeof(SpriteStudioPlugin));
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
