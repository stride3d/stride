// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Stride.Framework.Build.Storage;
using Stride.Framework.Diagnostics;

namespace Stride.BuildTool
{
    public class AndroidDeployAssetTask : Microsoft.Build.Utilities.Task
    {
        public ITaskItem[] Files { get; set; }

        public string DeploymentPath { get; set; }

        public override bool Execute()
        {
            var logger = Logger.GetLogger("AndroidDeployAssetTask");

            var fileMapping = new Dictionary<string, string>();
            for (int i = 0; i < Files.Length; ++i)
            {
                fileMapping[Files[i].GetMetadata("TargetPath")] = Files[i].ItemSpec;
            }

            var device = AndroidAdbUtilities.GetDevices().First();
            var externalStoragePath = AndroidAdbUtilities.GetExternalStoragePath(device);
            AndroidAdbUtilities.Synchronize(logger, device, fileMapping, externalStoragePath + "/" + DeploymentPath, "android-cache-" + device + ".tmp");

            return true;
        }
    }
}
