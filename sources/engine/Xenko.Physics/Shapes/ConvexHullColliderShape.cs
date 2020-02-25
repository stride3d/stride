// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Mathematics;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;

namespace Xenko.Physics
{
    public class ConvexHullColliderShape : ColliderShape
    {
        private readonly IReadOnlyList<Vector3> pointsList;
        private readonly IReadOnlyList<uint> indicesList;

        public ConvexHullColliderShape(IReadOnlyList<Vector3> points, IReadOnlyList<uint> indices, Vector3 scaling)
        {
            Type = ColliderShapeTypes.ConvexHull;
            Is2D = false;

            cachedScaling = scaling;

            pointsList = points;
            indicesList = indices;

            InternalShape = new BulletSharp.ConvexHullShape(PointsAsBullet())
            {
                LocalScaling = cachedScaling,
            };

            DebugPrimitiveMatrix = Matrix.Scaling(Vector3.One * DebugScaling);
        }

        public IReadOnlyList<Vector3> Points
        {
            get { return pointsList; }
        }
        public IReadOnlyList<uint> Indices
        {
            get { return indicesList; }
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            var verts = new VertexPositionNormalTexture[pointsList.Count];
            for (var i = 0; i < pointsList.Count; i++)
            {
                verts[i].Position = pointsList[i];
                verts[i].TextureCoordinate = Vector2.Zero;
                verts[i].Normal = Vector3.Zero;
            }

            var intIndices = indicesList.Select(x => (int)x).ToArray();

            ////calculate basic normals
            ////todo verify, winding order might be wrong?
            for (var i = 0; i < indicesList.Count; i += 3)
            {
                var i1 = intIndices[i];
                var i2 = intIndices[i + 1];
                var i3 = intIndices[i + 2];
                var a = verts[i1];
                var b = verts[i2];
                var c = verts[i3];
                var n = Vector3.Cross((b.Position - a.Position), (c.Position - a.Position));
                n.Normalize();
                verts[i1].Normal = verts[i2].Normal = verts[i3].Normal = n;
            }

            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(verts, intIndices, false);

            return new GeometricPrimitive(device, meshData).ToMeshDraw();
        }

        IEnumerable<BulletSharp.Math.Vector3> PointsAsBullet()
        {
            for (int i = 0; i < pointsList.Count; i++)
                yield return pointsList[i];
        }
    }
}
