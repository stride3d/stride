// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

using Xenko.Core;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// A composition material to blend different materials in a stack based manner.
    /// </summary>
    [DataContract("MaterialBlendLayers")]
    [Display("Material Layers")]
    public class MaterialBlendLayers : List<MaterialBlendLayer>, IMaterialLayers
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialBlendLayers"/> class.
        /// </summary>
        public MaterialBlendLayers()
        {
            Enabled = true;
        }

        [DataMemberIgnore]
        [DefaultValue(true)]
        public bool Enabled { get; set; }

        public virtual void Visit(MaterialGeneratorContext context)
        {
            if (!Enabled)
                return;

            foreach (var layer in this)
            {
                layer.Visit(context);
            }
        }
    }
}
