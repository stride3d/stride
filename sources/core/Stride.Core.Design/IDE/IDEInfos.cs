// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.VisualStudio;

namespace Stride.Core.IDE
{
    public static class IDEInfos
    {
        private static readonly Lazy<List<IDEInfo>> IDEInfosList = new Lazy<List<IDEInfo>>(CollectIDEs);
        
        public static IEnumerable<IDEInfo> AvailableIDEs => IDEInfosList.Value;
        
        public static readonly IDEInfo DefaultIDE = new IDEInfo(new Version("0.0"), "Default IDE", string.Empty);

        private static List<IDEInfo> CollectIDEs()
        {
            var ides = new List<IDEInfo>();
            
            ides.Add(DefaultIDE);

            ides.AddRange(RiderPathLocator.GetAllRiderPaths().Select(a => new IDEInfo(a.BuildNumber, a.Presentation, a.Path)));
            ides.AddRange(VisualStudioVersions.AvailableVisualStudioInstances.Select(a => new IDEInfo(a.Version, a.DisplayName, a.DevenvPath)));

            return ides;
        }
    }
}
