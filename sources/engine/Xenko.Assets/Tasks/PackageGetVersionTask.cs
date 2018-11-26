// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Diagnostics;

namespace Xenko.Assets.Tasks
{
    public class PackageGetVersionTask : Task
    {
        [Output]
        public string NuGetVersion { get; set; }

        [Output]
        public string NugetVersionSimpleNoRevision { get; set; }

        public override bool Execute()
        {
            NuGetVersion = XenkoVersion.NuGetVersion;

            var nugetVersionSimple = new Version(XenkoVersion.NuGetVersionSimple);
            nugetVersionSimple = new Version(nugetVersionSimple.Major, nugetVersionSimple.Minor, nugetVersionSimple.Build);
            NugetVersionSimpleNoRevision = nugetVersionSimple.ToString();
            return true;
        }
    }
}
