// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.SpriteStudio.Offline
{
    public class SpriteStudioAnim
    {
        public string Name;
        public int Fps;
        public int FrameCount;
        public Dictionary<string, NodeAnimationData> NodesData = new Dictionary<string, NodeAnimationData>();
    }

    public class SpriteStudioCell
    {
        public string Name;
        public RectangleF Rectangle;
        public Vector2 Pivot;
        public int TextureIndex;
    }
}
