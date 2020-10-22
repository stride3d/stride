// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.VisualStudio;

namespace Stride.Core.Editor
{
    public static class ExternalEditors
    {
        private static Lazy<List<EditorInfo>> IDEInfos = new Lazy<List<EditorInfo>>(BuildIDEInfos);
        
        public static IEnumerable<EditorInfo> AvailableEditors => IDEInfos.Value;
        
        public static EditorInfo DefaultIDE = new EditorInfo(new Version("0.0"), "Default IDE", string.Empty);

        private static List<EditorInfo> BuildIDEInfos()
        {
            var ideInfos = new List<EditorInfo>();
            
            ideInfos.Add(DefaultIDE);

            // Rider
            try
            {
                ideInfos.AddRange(RiderPathLocator.GetAllRiderPaths().Select(a => new EditorInfo(a.BuildNumber, a.Presentation, a.Path)));
            }
            catch { }
            
            ideInfos.AddRange(VisualStudioVersions.AvailableVisualStudioInstances.Select(a => new EditorInfo(a.Version, a.DisplayName, a.DevenvPath)));

            return ideInfos;
        }
    }
}
