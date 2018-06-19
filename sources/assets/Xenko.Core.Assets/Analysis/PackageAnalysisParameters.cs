// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Assets.Analysis
{
    /// <summary>
    /// Class PackageAnalysisParameters. This class cannot be inherited.
    /// </summary>
    public sealed class PackageAnalysisParameters : AssetAnalysisParameters
    {
        public bool IsPackageCheckDependencies { get; set; }
    }
}
