// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Xenko.Core.Assets;
using Xenko.Core.Diagnostics;
using Xenko.Assets.Tasks;

namespace Xenko.Assets.Tests
{
    public class TestPackageArchive
    {

        [Fact(Skip = "Need to check why it was disabled")]
        public void TestBasicPackageCreateSaveLoad()
        {
            // Override search path since we are in a unit test directory
            DirectoryHelper.PackageDirectoryOverride = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..");

            var defaultPackage = PackageStore.Instance.DefaultPackage;

            PackageArchive.Build(GlobalLogger.GetLogger("PackageArchiveTest"), defaultPackage, AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
