// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core;

namespace Xenko.Core.Assets.CompilerApp
{
    class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                // Set the XenkoDir environment variable
                var installDir = DirectoryHelper.GetInstallationDirectory("Xenko");
                Environment.SetEnvironmentVariable("XenkoDir", installDir);

                // Running first time? If yes, create nuget redirect package.
                var packageVersion = new PackageVersion(XenkoVersion.NuGetVersion);
                if (PackageStore.Instance.IsDevelopmentStore)
                {
                    PackageStore.Instance.CheckDeveloperTargetRedirects("Xenko", packageVersion, PackageStore.Instance.InstallationPath).Wait();
                }

                var packageBuilder = new PackageBuilderApp();
                var returnValue =  packageBuilder.Run(args);

                return returnValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception in AssetCompiler: {0}", ex);
                return 1;
            }
            finally
            {
                // Free all native library loaded from the process
                // We cannot free native libraries are some of them are loaded from static module initializer
                // NativeLibrary.UnLoadAll();
            }
        }
    }
}
