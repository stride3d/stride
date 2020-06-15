// Copyright (c) Stride contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Rendering;

using static Stride.DebugRendering.DebugRenderFeature;

namespace Stride.DebugRendering
{
    public class DebugRenderStageSelector : RenderStageSelector
    {

        [DefaultValue(RenderGroupMask.All)]
        public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

        [DefaultValue(null)]
        public RenderStage OpaqueRenderStage { get; set; }

        [DefaultValue(null)]
        public RenderStage TransparentRenderStage { get; set; }

        public override void Process(RenderObject renderObject)
        {
            if (((RenderGroupMask)(1U << (int)renderObject.RenderGroup) & RenderGroup) != 0)
            {
                var debugObject = (DebugRenderObject)renderObject;
                var renderStage = (debugObject.Stage == DebugRenderStage.Opaque) ? OpaqueRenderStage : TransparentRenderStage;
                if (renderStage != null)
                {
                    renderObject.ActiveRenderStages[renderStage.Index] = new ActiveRenderStage(null);
                }
            }
        }

    }
}
