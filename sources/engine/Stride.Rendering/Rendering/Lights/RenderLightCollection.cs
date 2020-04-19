// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Collections;
using Stride.Engine;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A list of <see cref="LightComponent"/> for a specified <see cref="RenderGroupMask"/>.
    /// </summary>
    public class RenderLightCollection : FastList<RenderLight>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLightCollection"/> class.
        /// </summary>
        public RenderLightCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderLightCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public RenderLightCollection(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Gets or sets the culling mask.
        /// </summary>
        /// <value>The culling mask.</value>
        public RenderGroupMask CullingMask { get; internal set; }

        /// <summary>
        /// Tags attached.
        /// </summary>
        public PropertyContainer Tags;
    }
}
