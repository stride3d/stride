// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Background
{
    public class RenderBackground : RenderObject
    {
        public bool Is2D;
        public Texture Texture;
        public float Intensity;
        public Quaternion Rotation;
    }
}
