// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Core.IO;

namespace Stride.Core.Assets.Analysis
{
    internal static class CommonAnalysis
    {
        internal static void UpdatePaths(IFileSynchronizable parentFileSync, IEnumerable<AssetReferenceLink> paths, AssetAnalysisParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException("parameters");

            var fileDirectory = parentFileSync.FullPath.GetParent();

            foreach (var assetReferenceLink in paths)
            {
                var currentLocation = (UPath)assetReferenceLink.Reference;

                // If we need to skip an attribute
                var upathAttribute = assetReferenceLink.Path.GetCustomAttribute<UPathAttribute>();
                if (upathAttribute != null && upathAttribute.RelativeTo == UPathRelativeTo.None)
                {
                    continue;
                }

                UPath newLocation = null;

                var uFile = currentLocation as UFile;
                var uDirectory = currentLocation as UDirectory;
                if (!string.IsNullOrEmpty(uFile))
                {
                    var previousLocationOnDisk = UPath.Combine(fileDirectory, uFile);

                    // If UseRelativeForUFile is used, then turn 
                    newLocation = parameters.ConvertUPathTo == UPathType.Relative ? previousLocationOnDisk.MakeRelative(fileDirectory) : previousLocationOnDisk;
                }
                else if (!string.IsNullOrEmpty(uDirectory))
                {
                    var previousDirectoryOnDisk = UPath.Combine(fileDirectory, uDirectory);

                    // If UseRelativeForUFile is used, then turn 
                    newLocation = parameters.ConvertUPathTo == UPathType.Relative ? previousDirectoryOnDisk.MakeRelative(fileDirectory) : previousDirectoryOnDisk;
                }
                // Only update location that are actually different
                if (currentLocation != newLocation)
                {
                    assetReferenceLink.UpdateReference(null, newLocation != null ? newLocation.FullPath : null);
                    if (parameters.SetDirtyFlagOnAssetWhenFixingUFile)
                    {
                        parentFileSync.IsDirty = true;
                    }
                }

                // Set dirty flag on asset if the uFile was previously absolute and 
                // SetDirtyFlagOnAssetWhenFixingAbsoluteUFile = true
                if ((currentLocation.IsAbsolute && parameters.ConvertUPathTo == UPathType.Absolute && parameters.SetDirtyFlagOnAssetWhenFixingAbsoluteUFile))
                {
                    parentFileSync.IsDirty = true;
                }
            }
        }
    }
}
