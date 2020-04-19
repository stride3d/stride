// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Assets.Models
{
    /// <summary>
    /// This interface represents an asset containing a model.
    /// </summary>
    public interface IModelAsset
    {
        /// <summary>
        /// The materials.
        /// </summary>
        /// <userdoc>
        /// The list of materials in the model.
        /// </userdoc>
        List<ModelMaterial> Materials { get; }
    }
}
