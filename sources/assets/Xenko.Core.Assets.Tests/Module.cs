// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xenko.Core.Reflection;

namespace Xenko.Core.Assets.Tests
{
    // Somehow it helps Resharper NUnit to run module initializer first (to determine unit test configuration).
    public class Module
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
            RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);

            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
        }
    }
}
