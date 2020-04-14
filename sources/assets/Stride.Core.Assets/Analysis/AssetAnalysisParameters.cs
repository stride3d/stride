// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;

namespace Stride.Core.Assets.Analysis
{
    /// <summary>
    /// Parameters for asset analysis.
    /// </summary>
    public class AssetAnalysisParameters
    {
        public bool IsLoggingAssetNotFoundAsError { get; set; }

        public bool IsProcessingAssetReferences { get; set; }

        public bool IsProcessingUPaths { get; set; }

        public bool SetDirtyFlagOnAssetWhenFixingAbsoluteUFile { get; set; }

        public bool SetDirtyFlagOnAssetWhenFixingUFile { get; set; }

        public UPathType ConvertUPathTo { get; set; }

        public virtual AssetAnalysisParameters Clone()
        {
            return (AssetAnalysisParameters)MemberwiseClone();
        }
    }
}
