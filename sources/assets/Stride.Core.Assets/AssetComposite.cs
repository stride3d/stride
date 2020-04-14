// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Base class for an asset that supports inheritance by composition.
    /// </summary>
    public abstract class AssetComposite : Asset, IAssetComposite
    {
        [Obsolete("The AssetPart struct might be removed soon")]
        public abstract IEnumerable<AssetPart> CollectParts();

        public abstract IIdentifiable FindPart(Guid partId);

        public abstract bool ContainsPart(Guid id);
    }
}
