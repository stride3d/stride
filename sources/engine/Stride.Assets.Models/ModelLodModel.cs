// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    /// <summary>
    /// Represents a <see cref="MaterialInstance"/> in a 
    /// </summary>
    [DataContract]
    public class ModelLodModel
    {
        /// <summary>
        /// Gets or sets the material slot name in a <see cref="ModelAsset"/>.
        /// </summary>
        /// <value>
        /// The material slot name.
        /// </value>
        /// <userdoc>The .</userdoc>
        /// <userdoc>The name of the material as written in the imported model and the reference to the corresponding material asset.</userdoc>
        [DataMember(10)]
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the material stored in this slot.
        /// </summary>
        /// <value>
        /// The material.
        /// </value>
        [DataMember(20)]
        public Model LodModel { get; set; }
    }
}
