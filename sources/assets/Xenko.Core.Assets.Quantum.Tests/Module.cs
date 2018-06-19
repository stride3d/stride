// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using NUnit.Core.Extensibility;
using Xenko.Core;
using Xenko.Core.Reflection;

namespace Xenko.Core.Assets.Quantum.Tests
{
    // Somehow it helps Resharper NUnit to run module initializer first (to determine unit test configuration).
    [NUnitAddin]
    public class Module : IAddin
    {
        public bool Install(IExtensionHost host)
        {
            return false;
        }

        [ModuleInitializer]
        internal static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
            AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
            RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);
        }
    }
}
