using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public class VoxelStorageContext
    {
        public GraphicsDevice device;
        public Vector3 Extents;
        public Vector3 Translation;
        public Vector3 VoxelSpaceTranslation;
        public float VoxelSize;
        public Matrix Matrix;

        public float RealVoxelSize()
        {
            Int3 resolution = Resolution();
            return Extents.X/(float)resolution.X;
        }
        public Int3 Resolution()
        {
            var resolution = Extents / VoxelSize;

            //Calculate closest power of 2 on each axis
            resolution.X = (float)Math.Pow(2, Math.Round(Math.Log(resolution.X, 2)));
            resolution.Y = (float)Math.Pow(2, Math.Round(Math.Log(resolution.Y, 2)));
            resolution.Z = (float)Math.Pow(2, Math.Round(Math.Log(resolution.Z, 2)));

            return new Int3((int)resolution.X, (int)resolution.Y, (int)resolution.Z);
        }
    }
}
