// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Engine;

namespace Stride.Rendering
{
    public abstract class TransparentRenderStageSelector : RenderStageSelector
    {
        [DefaultValue(RenderGroupMask.All)]
        public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

        [DefaultValue(null)]
        public RenderStage OpaqueRenderStage { get; set; }
        [DefaultValue(null)]
        public RenderStage TransparentRenderStage { get; set; }

        public string EffectName { get; set; }
    }
}
