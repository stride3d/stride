// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;

namespace Stride.Rendering.Rendering.MeshDataTool
{
    public abstract class MeshDataToolBase
    {
        protected Mesh mesh { get; set; }
        public MeshDataToolBase(Mesh srcMesh)
        {
            mesh = srcMesh;

            if(mesh.Draw.PrimitiveType != Graphics.PrimitiveType.TriangleList)
            {
                throw new Exception("Cant parse only Triangle Meshes");
            }
        }
        public abstract int getTotalVerticies();
        public abstract int getTotalIndicies();

        public abstract int[] getIndicies();

        public abstract Vector3[] getPositions();
        public abstract Vector2[] getUVs();
        public abstract Vector3[] getNormals();
        public abstract Vector4[] getTangents();

        public abstract Vector3 getPosition(int index);
        public abstract Vector2 getUV(int index);
        public abstract Vector3 getNormal(int index);
        public abstract Vector4 getTangent(int index);

        public int getTotalFaces()
        {
          return this.getTotalIndicies() / 3;
        }

        public int getTotalEdges()
        {
            throw new Exception("Not implmented yet");
        }

        public void getFaces()
        {
            throw new Exception("Not implmented yet");
        }

        public void getEdges()
        {
            throw new Exception("Not implmented yet");
        }
    }
}
