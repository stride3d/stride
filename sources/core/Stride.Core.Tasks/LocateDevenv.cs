// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.Setup.Configuration;
using Task = Microsoft.Build.Utilities.Task;

namespace Stride.Core.Tasks
{
    public class LocateDevenv : Task
    {
        [Output]
        public String DevenvPath { get; set; }

        public override bool Execute()
        {
            DevenvPath = FindDevenv(null);
            return DevenvPath != null;
        }

        internal static string FindDevenv(string msbuildPath)
        {
            var setupConfiguration = new SetupConfiguration() as ISetupConfiguration2;

            // When invoked from VS's own MSBuild the path/current process resolves the instance
            // directly. Under dotnet build, msbuildPath is the .NET SDK dir (no VS there), so
            // fall back to enumerating installed VS instances and picking the newest.
            return DevenvFrom(TryGet(() => setupConfiguration.GetInstanceForPath(msbuildPath)))
                ?? DevenvFrom(TryGet(() => setupConfiguration.GetInstanceForCurrentProcess()))
                ?? EnumerateNewestDevenv(setupConfiguration);
        }

        private static ISetupInstance TryGet(Func<ISetupInstance> get)
        {
            try { return get(); } catch { return null; }
        }

        private static string DevenvFrom(ISetupInstance instance)
        {
            if (instance == null) return null;
            try
            {
                var path = Path.Combine(instance.GetInstallationPath(), "Common7\\IDE\\devenv.exe");
                return File.Exists(path) ? path : null;
            }
            catch { return null; }
        }

        private static string EnumerateNewestDevenv(ISetupConfiguration2 setupConfiguration)
        {
            try
            {
                var e = setupConfiguration.EnumAllInstances();
                var batch = new ISetupInstance[1];
                string best = null, bestVersion = null;
                while (true)
                {
                    e.Next(1, batch, out int fetched);
                    if (fetched == 0) break;
                    var path = DevenvFrom(batch[0]);
                    if (path == null) continue;
                    string version = null;
                    try { version = batch[0].GetInstallationVersion(); } catch { }
                    if (bestVersion == null || string.CompareOrdinal(version, bestVersion) > 0)
                    {
                        bestVersion = version;
                        best = path;
                    }
                }
                return best;
            }
            catch
            {
                return null;
            }
        }
    }
}
