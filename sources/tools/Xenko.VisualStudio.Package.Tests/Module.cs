// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Xenko.Core.Assets;
using Xenko.Core;

namespace Xenko.VisualStudio.Package.Tests
{
    public class Module
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            // Override search path since we are in a unit test directory
            DirectoryHelper.PackageDirectoryOverride = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..");
        }
    }
}
