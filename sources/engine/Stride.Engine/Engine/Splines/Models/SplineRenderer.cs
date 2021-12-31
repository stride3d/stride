using Stride.Graphics;
using Stride.Rendering;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Models.Mesh;

namespace Stride.Engine.Splines
{
    [DataContract]
    public class SplineRenderer
    {
        private Entity splineMeshEntity;
        private Entity segmentsEntity;
        private Entity boundingBoxEntity;

        private bool segments;
        private bool boundingBox;
        private GraphicsDevice graphicsDevice;
        private bool boundingIter = false;

        public Material SegementsMaterial;

        public bool Segments
        {
            get { return segments; }
            set
            {
                segments = value;
            }
        }

        public Material BoundingBoxMaterial;
        public bool BoundingBox
        {
            get { return boundingBox; }
            set
            {
                boundingBox = value;
            }
        }

        public Entity Update(Spline spline, GraphicsDevice graphicsdevice, Vector3 splinePosition)
        {
            graphicsDevice = graphicsdevice;

            //Create the entities that hold the various debug meshes           
            splineMeshEntity ??= new Entity("SplineRenderer");

            segmentsEntity ??= new Entity("Segments");
            boundingBoxEntity ??= new Entity("BoundingBox");

            if (segmentsEntity.GetParent() == null)
            {
                splineMeshEntity.AddChild(segmentsEntity);
            }
            if (boundingBoxEntity.GetParent() == null)
            {
                splineMeshEntity.AddChild(boundingBoxEntity);
            }

            ClearChildren(segmentsEntity);
            ClearChildren(boundingBoxEntity);

            if (graphicsDevice == null || SegementsMaterial == null)
                return splineMeshEntity;

            var nodes = spline.SplineNodes;

            if (nodes?.Count > 1 && spline.Dirty)
            {
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
                        if (currentSplineNode == null) return splineMeshEntity;

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

                        if (Segments && SegementsMaterial != null)
                        {
                            DrawSplineSegments(splinePoints, graphicsDevice, splinePosition);
                        }

                        if (BoundingBox && BoundingBoxMaterial != null)
                        {
                            UpdateBoundingBox(currentSplineNode, graphicsDevice, splinePosition);
                        }
                    }
                }
            }

            spline.Dirty = false;

            return splineMeshEntity;
        }

        private void DrawSplineSegments(Vector3[] splinePoints, GraphicsDevice graphicsDevice, Vector3 position)
        {
            var splineMeshData = new SplineMeshData(splinePoints, graphicsDevice);
            var segments = new Entity() { new ModelComponent { Model = new Model { SegementsMaterial, new Mesh { Draw = splineMeshData.Build() }, }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false } };
            segmentsEntity.AddChild(segments);
            segments.Transform.Position -= position - segmentsEntity.Transform.Position;
        }

        private void UpdateBoundingBox(SplineNode splineNode, GraphicsDevice graphicsDevice, Vector3 position)
        {
            var boundingBoxMesh = new BoundingBoxMesh(graphicsDevice);
            boundingBoxMesh.Build(splineNode.BoundingBox);
            boundingIter = !boundingIter;

            var boundingBox = new Entity() { new ModelComponent { Model = new Model { BoundingBoxMaterial ?? SegementsMaterial, new Mesh { Draw = boundingBoxMesh.MeshDraw } }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false } };
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
