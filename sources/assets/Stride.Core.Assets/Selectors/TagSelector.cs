// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Selectors
{
    /// <summary>
    /// An <see cref="AssetSelector"/> using tags stored in <see cref="Asset.Tags"/>
    /// </summary>
    [DataContract("TagSelector")]
    public class TagSelector : AssetSelector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagSelector"/> class.
        /// </summary>
        public TagSelector()
        {
            Tags = new TagCollection();
        }

        /// <summary>
        /// Gets the tags that will be used to select an asset.
        /// </summary>
        /// <value>The tags.</value>
        public TagCollection Tags { get; private set; }

        public override IEnumerable<string> Select(PackageSession packageSession, IContentIndexMap contentIndexMap)
        {
            return packageSession.Packages
                .SelectMany(package => package.Assets) // Select all assets
                .Where(assetItem => assetItem.Asset.Tags.Any(tag => Tags.Contains(tag))) // Matches tags
                .Select(x => x.Location.FullPath); // Convert to string;
        }
    }
}
