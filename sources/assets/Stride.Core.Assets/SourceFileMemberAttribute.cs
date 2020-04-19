// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets
{
    /// <summary>
    /// An attribute indicating whether a member of an asset represents the path to a source file for this asset.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SourceFileMemberAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceFileMemberAttribute"/> class.
        /// </summary>
        /// <param name="updateAssetIfChanged">If true, the asset should be updated when the related source file changes.</param>
        public SourceFileMemberAttribute(bool updateAssetIfChanged)
        {
            UpdateAssetIfChanged = updateAssetIfChanged;
        }

        /// <summary>
        /// Gets whether the asset should be updated when the related source file changes.
        /// </summary>
        public bool UpdateAssetIfChanged { get; }

        /// <summary>
        /// Gets or sets whether this source file is optional for the compilation of the asset.
        /// </summary>
        public bool Optional { get; set; }
    }
}
