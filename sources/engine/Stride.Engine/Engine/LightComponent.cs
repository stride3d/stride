// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Graphics;
using Stride.Rendering.Lights;

namespace Stride.Engine
{
    /// <summary>
    /// Add a light to an <see cref="Entity"/>, that will be used during rendering.
    /// </summary>
    [DataContract("LightComponent")]
    [Display("Light", Expand = ExpandRule.Once)]
    // TODO GRAPHICS REFACTOR
    //[DefaultEntityComponentRenderer(typeof(LightComponentRenderer), -10)]
    [DefaultEntityComponentRenderer(typeof(LightProcessor))]
    [ComponentOrder(12000)]
    [ComponentCategory("Lights")]
    public sealed class LightComponent : ActivableEntityComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightComponent"/> class.
        /// </summary>
        public LightComponent()
        {
            Type = new LightDirectional();
            Intensity = 1.0f;
        }

        /// <summary>
        /// Gets or sets the type of the light.
        /// </summary>
        /// <value>The type of the light.</value>
        /// <userdoc>The type of the light</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Light", Expand = ExpandRule.Always)]
        public ILight Type { get; set; }

        /// <summary>
        /// Gets or sets the light intensity.
        /// </summary>
        /// <value>The light intensity.</value>
        /// <userdoc>The intensity of the light.</userdoc>
        [DataMember(30)]
        [DefaultValue(1.0f)]
        public float Intensity { get; set; }
    }
}
