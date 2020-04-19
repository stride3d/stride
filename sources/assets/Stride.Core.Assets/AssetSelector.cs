// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets
{
    /// <summary>
    /// An asset selector
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class AssetSelector
    {
        public abstract IEnumerable<string> Select(PackageSession packageSession, IContentIndexMap contentIndexMap);
    }
}
