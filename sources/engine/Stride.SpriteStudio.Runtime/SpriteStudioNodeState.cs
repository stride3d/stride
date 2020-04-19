// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.SpriteStudio.Runtime
{
    [DataContract]
    public class SpriteStudioNodeState
    {
        public SpriteStudioNodeState()
        {
            DefaultPixelsPerUnit = new Vector2(Sprite.DefaultPixelsPerUnit);
        }

        public Matrix LocalTransform;

        public Matrix ModelTransform;

        public int HFlipped;

        public int VFlipped;

        public Vector2 Position;

        public float RotationZ;

        public int Priority;

        public Vector2 Scale;

        public float Transparency;

        public int Hide;

        public int SpriteId;

        public Color4 BlendColor;
        public SpriteStudioBlending BlendType;
        public float BlendFactor;

        [DataMemberIgnore]
        public Sprite Sprite;

        [DataMemberIgnore]
        public SpriteStudioNode BaseNode;

        [DataMemberIgnore]
        public Vector2 DefaultPixelsPerUnit;

        [DataMemberIgnore]
        public SpriteStudioNodeState ParentNode;

        [DataMemberIgnore]
        public List<SpriteStudioNodeState> ChildrenNodes { get; } = new List<SpriteStudioNodeState>();

        [DataMemberIgnore]
        public float FinalTransparency;

        internal void UpdateTransformation()
        {
            var unit = Sprite?.PixelsPerUnit ?? DefaultPixelsPerUnit;
            var scale = Matrix.Scaling(HFlipped != 0 ? -Scale.X : Scale.X, VFlipped != 0 ? -Scale.Y : Scale.Y, 1.0f);
            var rot = Matrix.RotationZ(RotationZ);
            var pos = Matrix.Translation(Position.X / unit.X, Position.Y / unit.Y, 0.0f);
            LocalTransform = scale*rot*pos;

            FinalTransparency = Transparency;

            if (ParentNode != null)
            {
                Matrix.Multiply(ref LocalTransform, ref ParentNode.ModelTransform, out ModelTransform);

                if (BaseNode.AlphaInheritance)
                {
                    FinalTransparency = Transparency * ParentNode.FinalTransparency;
                }
                if (BaseNode.FlphInheritance)
                {
                    HFlipped = ParentNode.HFlipped;
                }
                if (BaseNode.FlpvInheritance)
                {
                    VFlipped = ParentNode.VFlipped;
                }
                if (BaseNode.HideInheritance)
                {
                    Hide = ParentNode.Hide;
                }
            }
            else
            {
                ModelTransform = LocalTransform;
            }

            foreach (var childrenNode in ChildrenNodes)
            {
                childrenNode.UpdateTransformation();
            }
        }
    }
}
