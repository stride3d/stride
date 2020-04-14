// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Graphics;

namespace Stride.Rendering.Compositing
{
    public struct RenderTargetDescription : IEquatable<RenderTargetDescription>
    {
        public IRenderTargetSemantic Semantic;

        public PixelFormat Format;

        public bool Equals(RenderTargetDescription other)
        {
            return Semantic.GetType() == other.Semantic.GetType() && Format == other.Format;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderTargetDescription && Equals((RenderTargetDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Semantic?.GetType().GetHashCode() ?? 0) * 397) ^ (int)Format;
            }
        }

        public static bool operator ==(RenderTargetDescription left, RenderTargetDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RenderTargetDescription left, RenderTargetDescription right)
        {
            return !left.Equals(right);
        }
    }
}
