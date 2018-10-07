// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xenko.Core;
using Xenko.Core.Assets;
using Xunit;

// We run test one by one (various things are not thread-safe)
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Xenko.Samples.Tests
{
    class SampleTestsData
    {
#if TEST_ANDROID
        public const PlatformType TestPlatform = PlatformType.Android;
#elif TEST_IOS
        public const PlatformType TestPlatform = PlatformType.iOS;
#else
        public const PlatformType TestPlatform = PlatformType.Windows;
#endif

        [ModuleInitializer]
        public static void Initialize()
        {
            // Override search path since we are in a unit test directory
            DirectoryHelper.PackageDirectoryOverride = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..");

            // Running first time? If yes, create nuget redirect package.
            var packageVersion = new PackageVersion(XenkoVersion.NuGetVersion);
            if (PackageStore.Instance.IsDevelopmentStore)
            {
                PackageStore.Instance.CheckDeveloperTargetRedirects("Xenko", packageVersion, PackageStore.Instance.InstallationPath).Wait();
            }
        }
    }
}
