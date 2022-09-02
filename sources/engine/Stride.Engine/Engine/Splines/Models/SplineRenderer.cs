//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;
using Stride.Rendering;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Extensions;

namespace Stride.Engine.Splines.Models
{
    [DataContract]
    public class SplineRenderer
    {
        private bool segments;
        private bool boundingBox;
        private bool nodes;
        private Entity splineMeshEntity;

        public delegate void SplineRendererSettingsUpdatedHandler();
        public event SplineRendererSettingsUpdatedHandler OnSplineRendererSettingsUpdated;

        /// <summary>
        /// Display spline curve mesh
        /// </summary>
        [Display(10, "Show segments")]
        public bool Segments
        {
            get
            {
                return segments;
            }
            set
            {
                segments = value;

                OnSplineRendererSettingsUpdated?.Invoke();
            }
        }

        /// <summary>
        /// The material used by the spline mesh
        /// </summary>
        [Display(20, "Segments material")]
        public Material SegmentsMaterial;

        /// <summary>
        /// Display Spline nodes
        /// </summary>
        [Display(23, "Show nodes")]
        public bool Nodes
        {
            get
            {
                return nodes;
            }
            set
            {
                nodes = value;

                OnSplineRendererSettingsUpdated?.Invoke();
            }
        }

        /// <summary>
        /// The material used by the spline nodes mesh
        /// </summary>
        [Display(26, "Nodes material")]
        public Material NodesMaterial;

        /// <summary>
        /// Display the bounding boxes of each node and the entire spline
        /// </summary>
        [Display(30, "Show boundingbox")]
        public bool BoundingBox
        {
            get { return boundingBox; }
            set
            {
                boundingBox = value;

                OnSplineRendererSettingsUpdated?.Invoke();
            }
        }

        /// <summary>
        /// The material used by the spline boundingboxes
        /// </summary>
        [Display(40, "Boundingbox material")]
        public Material BoundingBoxMaterial;

        /// <summary>
        /// Creates an entity with a mesh that visualises various spline parts like segments and bounding boxes
        /// </summary>
        /// <param name="spline"></param>
        /// <param name="graphicsDevice"></param>
        /// <param name="splinePosition"></param>
        /// <returns>An entity with sub entities containing various meshes to visualise the spline</returns>
        public Entity Create(Spline spline, GraphicsDevice graphicsDevice, Entity entity)
        {
            splineMeshEntity = new Entity("SplineRenderer");

            if (graphicsDevice == null || SegmentsMaterial == null || spline == null)
                return splineMeshEntity;

            var nodes = spline.SplineNodes;

            if (nodes?.Count > 1)
            {
                var totalNodesCount = nodes.Count;
                for (var i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNode = nodes[i];

                    if (currentSplineNode == null)
                    {
                        break;
                    }

                    // Show spline node meshes
                    if (Nodes)
                    {
                        CreateSplineNodeMesh(i.ToString(), graphicsDevice, currentSplineNode.WorldPosition, entity, splineMeshEntity);
                    }

                    // Don't create a mesh when it is the last node and Loop is disabled
                    if (i == totalNodesCount - 1 && !spline.Loop)
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
                        for (var j = 0; j < curvePointsInfo.Length; j++)
                        {
                            if (curvePointsInfo[j] == null)
                                break;
                            splinePoints[j] = curvePointsInfo[j].Position;
                        }

                        // Show spline segment mesh
                        if (Segments)
                        {
                            CreateSegmentLineMesh(i.ToString(), splinePoints, graphicsDevice, entity, splineMeshEntity);
                        }

                        // Show bounding box of spline segment
                        if (BoundingBox)
                        {
                            UpdateSegmentBoundingBoxMesh(i.ToString(), currentSplineNode.BoundingBox, graphicsDevice, entity, splineMeshEntity);
                        }
                    }
                }
            }

            // Show bounding box of entire splines
            if (BoundingBox)
            {
                UpdateSegmentBoundingBoxMesh("Spline", spline.BoundingBox, graphicsDevice, entity, splineMeshEntity, SegmentsMaterial);
            }

            return splineMeshEntity;
        }

        private void CreateSegmentLineMesh(string description, Vector3[] splinePoints, GraphicsDevice graphicsDevice, Entity splineEntity, Entity splineMeshEntity)
        {
            var splineMeshData = new SplineMeshData(splinePoints, graphicsDevice);
            var segmentsEntity = new Entity($"Segment_{description}") { new ModelComponent { Model = new Model { SegmentsMaterial, new Mesh { Draw = splineMeshData.Build() }, }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false } };
            splineMeshEntity.AddChild(segmentsEntity);
            segmentsEntity.Transform.Position -= splineEntity.Transform.Position;
        }

        private void UpdateSegmentBoundingBoxMesh(string description, BoundingBox boundingBox, GraphicsDevice graphicsDevice, Entity splineEntity, Entity splineMeshEntity, Material overrideMaterial = null)
        {
            var boundingBoxMesh = new BoundingBoxMesh(graphicsDevice);
            boundingBoxMesh.Build(boundingBox);

            var boundingBoxMaterial = overrideMaterial ?? BoundingBoxMaterial ?? SegmentsMaterial;
            var boundingBoxEntity = new Entity($"BoundingBox_{description}") { new ModelComponent { Model = new Model { boundingBoxMaterial, new Mesh { Draw = boundingBoxMesh.MeshDraw } }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false } };
            splineMeshEntity.AddChild(boundingBoxEntity);
            boundingBoxEntity.Transform.Position -= splineEntity.Transform.Position;
        }

        private void CreateSplineNodeMesh(string description, GraphicsDevice graphicsDevice, Vector3 splineNodeWorldPosition, Entity splineEntity, Entity splineMeshEntity, Material overrideMaterial = null)
        { 
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(graphicsDevice, 0.15f, 8).ToMeshDraw();
            var splineNodeMaterial = overrideMaterial ?? NodesMaterial ?? SegmentsMaterial;
            var splineNodeEntity = new Entity($"SplineNode_{description}") { new ModelComponent { Model = new Model { splineNodeMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false } };
            splineMeshEntity.AddChild(splineNodeEntity);
            splineNodeEntity.Transform.Position -= splineEntity.Transform.WorldMatrix.TranslationVector - splineNodeWorldPosition;
        }
    }
}
