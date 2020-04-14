// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Navigation
{
    [DataContract]
    internal class NavigationMeshInputBuilder
    {
        public BoundingBox BoundingBox = BoundingBox.Empty;
        public List<Vector3> Points = new List<Vector3>();
        public List<int> Indices = new List<int>();

        /// <summary>
        /// Appends another vertex data builder
        /// </summary>
        /// <param name="other"></param>
        public void AppendOther(NavigationMeshInputBuilder other)
        {
            // Copy vertices
            int vbase = Points.Count;
            for (int i = 0; i < other.Points.Count; i++)
            {
                Vector3 point = other.Points[i];
                Points.Add(point);
                BoundingBox.Merge(ref BoundingBox, ref point, out BoundingBox);
            }

            // Copy indices with offset applied
            foreach (int index in other.Indices)
                Indices.Add(index + vbase);
        }

        public void AppendArrays(Vector3[] vertices, int[] indices, Matrix objectTransform)
        {
            // Copy vertices
            int vbase = Points.Count;
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = Vector3.Transform(vertices[i], objectTransform).XYZ();
                Points.Add(vertex);
                BoundingBox.Merge(ref BoundingBox, ref vertex, out BoundingBox);
            }

            // Copy indices with offset applied
            foreach (int index in indices)
            {
                Indices.Add(index + vbase);
            }
        }

        public void AppendArrays(Vector3[] vertices, int[] indices)
        {
            // Copy vertices
            int vbase = Points.Count;
            for (int i = 0; i < vertices.Length; i++)
            {
                Points.Add(vertices[i]);
                BoundingBox.Merge(ref BoundingBox, ref vertices[i], out BoundingBox);
            }

            // Copy indices with offset applied
            foreach (int index in indices)
            {
                Indices.Add(index + vbase);
            }
        }

        /// <summary>
        /// Appends local mesh data transformed with and object transform
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="objectTransform"></param>
        public void AppendMeshData(GeometricMeshData<VertexPositionNormalTexture> meshData, Matrix objectTransform)
        {
            // Transform box points
            int vbase = Points.Count;
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                VertexPositionNormalTexture point = meshData.Vertices[i];
                point.Position = Vector3.Transform(point.Position, objectTransform).XYZ();
                Points.Add(point.Position);
                BoundingBox.Merge(ref BoundingBox, ref point.Position, out BoundingBox);
            }

            if (meshData.IsLeftHanded)
            {
                // Copy indices with offset applied
                for (int i = 0; i < meshData.Indices.Length; i += 3)
                {
                    Indices.Add(meshData.Indices[i] + vbase);
                    Indices.Add(meshData.Indices[i + 2] + vbase);
                    Indices.Add(meshData.Indices[i + 1] + vbase);
                }
            }
            else
            {
                // Copy indices with offset applied
                for (int i = 0; i < meshData.Indices.Length; i++)
                {
                    Indices.Add(meshData.Indices[i] + vbase);
                }
            }
        }
    }
}
