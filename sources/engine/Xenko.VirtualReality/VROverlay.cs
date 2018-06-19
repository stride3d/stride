// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.VirtualReality
{
    public abstract class VROverlay
    {
        public Vector3 Position;

        public Quaternion Rotation;

        public Vector2 SurfaceSize;

        public bool FollowHeadRotation;

        public virtual bool Enabled { get; set; } = true;

        public int Width { get; protected set; }

        public int Height { get; protected set; }

        public abstract void Dispose();

        public abstract void UpdateSurface(CommandList commandList, Texture texture);
    }
}
