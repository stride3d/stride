// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using Stride.Core.Reflection;

namespace Stride.Core.Mathematics
{
    /// <summary>
    /// Module initializer.
    /// </summary>
    internal class Module
    {
        /// <summary>
        /// Module initializer.
        /// </summary>
        [ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
