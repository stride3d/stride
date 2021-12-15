// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Rendering;
using Stride.Rendering.MeshDecimator;
using Stride.Rendering.Rendering.MeshDataTool;
using Stride.Rendering.Rendering.MeshDecimator.Math;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Models
{
    public partial class ImportModelCommand
    {
        public float LodQuality { get; set; }
        public int LodLevel { get; set; }

        private object ExportLOD(ICommandContext commandContext, ContentManager contentManager)
        {
            Model lodModelSrc = (Model)ExportModel(commandContext, contentManager);

            var dstModel = new Model();
            dstModel.BoundingBox = lodModelSrc.BoundingBox;
            dstModel.BoundingSphere = lodModelSrc.BoundingSphere;

            int meshId = 1;
            foreach (var mesh in lodModelSrc.Meshes)
            {
                DateTime start = DateTime.Now;
                var convertedMesh = new Mesh();

                //read the buffers from source mesh
                var dt = new MeshDataToolStorage(mesh, contentManager);

                var totalIndicies = dt.getTotalIndicies();
                var totalVertices = dt.getTotalVerticies();
                int currentTriangleCount = totalIndicies / 3;

                float newQuality = MathHelper.Clamp01(this.LodQuality);
                int newTriangleCount = (int)MathF.Ceiling(currentTriangleCount * newQuality);

                var sourceMesh = new MeshDecimatorData(
                    dt.getPositions().Select(d => new Double3(d.X, d.Y, d.Z)).ToArray(),
                    dt.getIndicies());

                sourceMesh.UV1 = dt.getUVs();
                sourceMesh.Tangents = dt.getTangents();
                sourceMesh.Normals = dt.getNormals();

                var n = sourceMesh.Normals.Length;

                commandContext.Logger.Info(string.Format("[LOD][{0}][{4}] Start generate  => Quality {1} => Triangles {2} to {3}", this.LodLevel, newQuality, currentTriangleCount, newTriangleCount, meshId));
                var algorithm = MeshDecimator.CreateAlgorithm(Algorithm.FastQuadricMesh);
                var destMesh = MeshDecimator.DecimateMesh(algorithm, sourceMesh, newTriangleCount);
                commandContext.Logger.Info(string.Format("[LOD][{0}][{2}] Total Triangle Count", this.LodLevel, (destMesh.Indices.Length / 3), meshId));

                //create mesh surface
                var surface = new MeshSurfaceTool();
                surface.SetPositions(destMesh.Vertices.Select(p =>
                    new Vector3(
                        Convert.ToSingle(p.X),
                        Convert.ToSingle(p.Y),
                        Convert.ToSingle(p.Z))
                ).ToArray());

                if (destMesh.UV1 != null && destMesh.UV1.Length > 0)
                    surface.SetUvs(destMesh.UV1);

                if (destMesh.Normals != null && destMesh.Normals.Length > 0)
                    surface.SetNormals(destMesh.Normals);

                if (destMesh.Tangents != null && destMesh.Tangents.Length > 0)
                    surface.SetTangents(destMesh.Tangents);

                surface.SetIndices(destMesh.Indices);

                //generate the draw mesh
                var draw = surface.Generate();

                convertedMesh.Draw = draw;
                convertedMesh.Draw.DrawCount = destMesh.Indices.Length;
                convertedMesh.BoundingBox = mesh.BoundingBox;
                convertedMesh.BoundingSphere = mesh.BoundingSphere;

                dstModel.Add(convertedMesh);
                meshId++;

                DateTime end = DateTime.Now;
                TimeSpan ts = (end - start);

                commandContext.Logger.Info(string.Format("[LOD][{0}][{1}] Mesh simplify ends with {0}ms execution time.", ts.TotalMilliseconds, meshId));
            }

            return dstModel;
        }
    }
}
