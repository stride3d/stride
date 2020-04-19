// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.SpriteStudio.Runtime
{
    public class RenderSpriteStudio : RenderObject
    {
        public Matrix WorldMatrix;

        public SpriteStudioSheet Sheet;
        public List<SpriteStudioNodeState> SortedNodes = new List<SpriteStudioNodeState>();
    }
}
