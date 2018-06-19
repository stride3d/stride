// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Tracking;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;

namespace Xenko.Core.Assets.Quantum
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Make sure that this assembly is registered
            AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
        }
    }
}
