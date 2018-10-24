// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Xenko.Core;
using Xenko.Core.IO;

namespace Xenko.Core.Assets.Tests
{
    public class TestPackageStore
    {
        [Fact]
        public void TestDefault()
        {
            // Initialize a default package manager that will use the 
            var packageManager = PackageStore.Instance;

            var packageFileName = packageManager.GetPackageWithFileName("Xenko");

            Assert.True(File.Exists(packageFileName), "Unable to find default package file [{0}]".ToFormat(packageFileName));
        }

        //[Fact]
        //public void TestRemote()
        //{
        //    // Only work if the remote is correctly setup using the store.config nuget file
        //    var packageManager = new PackageStore();
        //    var installedPackages = packageManager.GetInstalledPackages().ToList();

        //    foreach (var packageMeta in packageManager.GetPackages())
        //    {
        //        Console.WriteLine("Package {0} {1}", packageMeta.Name, packageMeta.Version);
        //    }

        //}
         
    }
}
