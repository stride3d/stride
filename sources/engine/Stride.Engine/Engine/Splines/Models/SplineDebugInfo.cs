using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Collections.Generic;
using Stride.Rendering.Materials.ComputeColors;
using System.Linq;
using Stride.Engine.Splines.Models.Mesh;

namespace Stride.Engine.Splines
{
    [DataContract]
    public class SplineDebugInfo
    {

        private Entity splineDebugEntity;
        private Entity segmentsEntity;
        private Entity boundingBoxEntity;

        private bool segments;
        private Color segmentsColor = new(0, 100, 0);
        private bool boundingBox;

        private bool boundingIter = false;

        public SplineDebugInfo()
        {
            //Create the entities that hold the various debug meshes           
            segmentsEntity = new Entity("Segments");
            boundingBoxEntity = new Entity("BoundingBox");

            splineDebugEntity = new Entity("SplineDebug");
            splineDebugEntity.AddChild(segmentsEntity);
            splineDebugEntity.AddChild(boundingBoxEntity);
        }

        public bool Segments
        {
            get { return segments; }
            set
            {
                segments = value;
            }
        }

        public Color SegmentsColor
        {
            get { return segmentsColor; }
            set
            {
                segmentsColor = value;

            }
        }

        public bool BoundingBox
        {
            get { return boundingBox; }
            set
            {
                boundingBox = value;
            }
        }

        public Entity UpdateSplineDebugInfo(Spline spline, GraphicsDevice graphicsDevice, Material splineMaterial, Vector3 splinePosition)
        {
            var nodes = spline.SplineNodes;

            if (nodes?.Count > 1 && spline.Dirty)
            {
                splineMaterial.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(segmentsColor));

                ClearChildren(segmentsEntity);
                ClearChildren(boundingBoxEntity);

                var totalNodesCount = nodes.Count;
                for (int i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNode = nodes[i];

                    if (currentSplineNode == null)
                    {
                        break;
                    }

                    if (i == totalNodesCount - 1 && !spline.Loop) // Dont debugdraw when it is the last node and Loop is disabled
                    {
                        break;
                    }

                    if (Segments || BoundingBox)
                    {
                        if (currentSplineNode == null) return splineDebugEntity;

                        var curvePointsInfo = currentSplineNode.GetBezierPoints();

                        if (curvePointsInfo?[0] == null)
                            break;

                        var splinePoints = new Vector3[curvePointsInfo.Length];
                        for (int j = 0; j < curvePointsInfo.Length; j++)
                        {
                            if (curvePointsInfo[j] == null)
                                break;
                            splinePoints[j] = curvePointsInfo[j].Position;
                        }

                        if (Segments)
                        {
                            DrawSplineSegments(splinePoints, graphicsDevice, splineMaterial, splinePosition);
                        }

                        if (BoundingBox)
                        {
                            UpdateBoundingBox(currentSplineNode, graphicsDevice, splineMaterial, splinePosition);
                        }
                    }
                }

                spline.Dirty = false;
            }

            return splineDebugEntity;
        }
    
        private void DrawSplineSegments(Vector3[] splinePoints, GraphicsDevice graphicsDevice, Material splineMaterial, Vector3 position)
        {
            var splineMeshData = new SplineMeshData(splinePoints, graphicsDevice);
            var segments = new Entity() { new ModelComponent { Model = new Model { splineMaterial, new Mesh { Draw = splineMeshData.Build() } }, RenderGroup = RenderGroup.Group4 } };
            segmentsEntity.AddChild(segments);
            segments.Transform.Position -= position - segmentsEntity.Transform.Position;
        }

        private void UpdateBoundingBox(SplineNode splineNode, GraphicsDevice graphicsDevice, Material splineMaterial, Vector3 position)
        {
            var boundingBoxMesh = new BoundingBoxMesh(graphicsDevice);
            boundingBoxMesh.Build(splineNode.BoundingBox);
            boundingIter = !boundingIter;

            var boundingBox = new Entity(){ new ModelComponent{ Model = new Model { splineMaterial, new Mesh { Draw = boundingBoxMesh.MeshDraw } },RenderGroup = RenderGroup.Group4 } };
            boundingBoxEntity.AddChild(boundingBox);
            boundingBox.Transform.Position -= position - boundingBox.Transform.Position;
        }

        private void ClearChildren(Entity entity)
        {
            var children = entity.GetChildren();
            foreach (var child in children)
            {
                entity?.RemoveChild(child);
            }
        }
    }
}
