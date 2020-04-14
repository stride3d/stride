using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Voxels
{
    static class VoxelUtils
    {
        public static bool DisposeBufferBySpecs(Xenko.Graphics.Buffer buf, int count)
        {
            if (buf == null || buf.ElementCount != count)
            {
                if (buf != null)
                    buf.Dispose();

                return true;
            }
            return false;
        }

        public static bool TextureDimensionsEqual(Texture tex, Vector3 dim)
        {
            return (tex.Width == dim.X &&
                    tex.Height == dim.Y &&
                    tex.Depth == dim.Z);
        }
        public static bool DisposeTextureBySpecs(Xenko.Graphics.Texture tex, Vector3 dim, Xenko.Graphics.PixelFormat pixelFormat)
        {
            if (tex == null || !TextureDimensionsEqual(tex, dim) || tex.Format != pixelFormat)
            {
                if (tex != null)
                    tex.Dispose();

                return true;
            }
            return false;
        }
        public static bool DisposeTextureBySpecs(Xenko.Graphics.Texture tex, Vector3 dim, Xenko.Graphics.PixelFormat pixelFormat, MultisampleCount samples)
        {
            if (tex == null || !TextureDimensionsEqual(tex, dim) || tex.Format != pixelFormat || tex.MultisampleCount != samples)
            {
                if (tex != null)
                    tex.Dispose();

                return true;
            }
            return false;
        }
    }
}
