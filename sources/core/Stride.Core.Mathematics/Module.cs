// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using Xenko.Core.Reflection;

namespace Xenko.Core.Mathematics
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
