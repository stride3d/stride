// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Rendering.LightProbes;

namespace Stride.Engine
{
    [DataContract("LightProbeComponent")]
    [Display("Light probe", Expand = ExpandRule.Once)]
    [DefaultEntityComponentRenderer(typeof(LightProbeProcessor))]
    [ComponentOrder(15000)]
    [ComponentCategory("Lights")]
    public class LightProbeComponent : EntityComponent
    {
        [Display(Browsable = false)]
        [NonIdentifiableCollectionItems]
        public FastList<Color3> Coefficients { get; set; }
    }
}
