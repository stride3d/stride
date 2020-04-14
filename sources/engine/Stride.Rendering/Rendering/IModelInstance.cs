// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;

namespace Stride.Rendering
{
    /// <summary>
    /// Instance of a model with its parameters.
    /// </summary>
    public interface IModelInstance
    {
        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        Model Model { get; }

        /// <summary>
        /// Gets the materials.
        /// </summary>
        /// <value>
        /// The materials.
        /// </value>
        IndexingDictionary<Material> Materials { get; }
    }
}
