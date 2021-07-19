// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines;
using Stride.Graphics;
using Stride.Navigation;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo to display the bounding boxes for navigation meshes inside the editor as a gizmo. 
    /// this gizmo uses scale as the extent of the bounding box and is not affected by rotation
    /// </summary>
    //[GizmoComponent(typeof(SplineNodeComponent), false)]
    //public class SplineNodeGizmo : EntityGizmo<SplineNodeComponent>
    //{
        //private Entity debugEntity;
        //private Entity debugEntityNodeLink;
        //private Entity debugEntitySegments;
        //private Entity debugEntityOrbs;
        //private Entity debugEntityOut;
        //private Entity debugEntityIn;
        //private float updateFrequency = 0.2f;
        //private float updateTimer = 0.2f;

        //public SplineNodeGizmo(EntityComponent component) : base(component)
        //{
        //}

        //protected override Entity Create()
        //{
        //    debugEntity = new Entity();
        //    debugEntityNodeLink = new Entity();
        //    debugEntitySegments = new Entity();
        //    debugEntityOrbs = new Entity();
        //    debugEntityOut = new Entity();
        //    debugEntityIn = new Entity();
        //    debugEntity.AddChild(debugEntityNodeLink);
        //    debugEntity.AddChild(debugEntitySegments);
        //    debugEntity.AddChild(debugEntityOrbs);
        //    debugEntity.AddChild(debugEntityOut);
        //    debugEntity.AddChild(debugEntityIn);
        //    return debugEntity;
        //}

        //public override void Update()
        //{
        //    //updateTimer += (float)Game.UpdateTime.Elapsed.TotalSeconds;

        //    //if (ContentEntity == null || GizmoRootEntity == null)
        //    //    return;

        //    //if (updateTimer > updateFrequency)
        //    //{
        //    //    updateTimer = 0;
        //    //    return;
        //    //}

        //    //// calculate the world matrix of the gizmo so that it is positioned exactly as the corresponding scene entity
        //    //// except the scale that is re-adjusted to the gizmo desired size (gizmo are insert at scene root so LocalMatrix = WorldMatrix)
        //    //Vector3 scale;
        //    //Quaternion rotation;
        //    //Vector3 translation;
        //    //ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rotation, out translation);

        //    //// Translation and Scale but no rotation on bounding boxes
        //    //GizmoRootEntity.Transform.Position = translation;
        //    //GizmoRootEntity.Transform.Scale = 1 * scale;
        //    //GizmoRootEntity.Transform.UpdateWorldMatrix();

        //    //if (Component.Info.IsDirty && Component.Next != null)
        //    //{
        //    //    ClearChildren(debugEntitySegments);
        //    //    ClearChildren(debugEntityOrbs);

        //    //    debugEntitySegments.RemoveAll<ModelComponent>();
        //    //    debugEntityNodeLink.RemoveAll<ModelComponent>();
        //    //    debugEntityOut.RemoveAll<ModelComponent>();
        //    //    debugEntityIn.RemoveAll<ModelComponent>();
        //    //    Component.Info.IsDirty = false;

        //    //    if (Component.Info.NodesLink)
        //    //    {
        //    //        DebugNodeLinks();
        //    //    }

        //    //    if (Component.Info.Segments || Component.Info.Points)
        //    //    {
        //    //        var splinePointsInfo = Component.GetSplineNode().GetSplinePointInfo();
        //    //        var splinePoints = new Vector3[splinePointsInfo.Length];
        //    //        for (int i = 0; i < splinePointsInfo.Length; i++)
        //    //        {
        //    //            splinePoints[i] = splinePointsInfo[i].position;
        //    //        }

        //    //        if (Component.Info.Points)
        //    //        {
        //    //            CreateSplinePoints(splinePoints);
        //    //        }

        //    //        if (Component.Info.Segments)
        //    //        {
        //    //            CreateSplineSegments(splinePoints);
        //    //        }
        //    //    }

        //    //    if (Component.Info.OutHandler)
        //    //    {
        //    //        CreateOutHandler();
        //    //    }

        //    //    if (Component.Info.InHandler)
        //    //    {
        //    //        CreateInHandler();
        //    //    }
        //    //}
        //}

        //private void CreateSplineSegments(Vector3[] splinePoints)
        //{
        //    //var localPoints = new Vector3[splinePoints.Length];
        //    //for (int i = 0; i < splinePoints.Length - 1; i++)
        //    //{
        //    //    localPoints[i] = debugEntity.Transform.WorldToLocal(splinePoints[i]);
        //    //}

        //    //var bezierSegmentsMesh = new LineMesh(GraphicsDevice);
        //    //bezierSegmentsMesh.Build(localPoints);
        //    //debugEntitySegments.RemoveAll<ModelComponent>();
        //    //debugEntitySegments.Add(
        //    //    new ModelComponent
        //    //    {
        //    //        Model = new Model
        //    //        {
        //    //                GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Orange),
        //    //                new Mesh { Draw = bezierSegmentsMesh.MeshDraw }
        //    //        },
        //    //        RenderGroup = RenderGroup
        //    //    }
        //    //);

        //    ClearChildren(debugEntitySegments);

        //    var localPoints = new Vector3[splinePoints.Length];
        //    for (int i = 0; i < splinePoints.Length; i++)
        //    {
        //        localPoints[i] = debugEntity.Transform.WorldToLocal(splinePoints[i]);
        //    }

        //    for (int i = 0; i < localPoints.Length - 1; i++)
        //    {
        //        var lineMesh = new LineMesh(GraphicsDevice);
        //        lineMesh.Build(new Vector3[2] { localPoints[i], localPoints[i + 1] });

        //        var segment = new Entity()
        //        {
        //            new ModelComponent
        //            {
        //                Model = new Model
        //                {
        //                    GizmoUniformColorMaterial.Create(GraphicsDevice, i % 2 == 0 ? Color.Orange: Color.OrangeRed),
        //                    new Mesh { Draw = lineMesh.MeshDraw }
        //                },
        //                RenderGroup = RenderGroup,
        //            }
        //        };

        //        debugEntitySegments.AddChild(segment);
        //        segment.Transform.Position = debugEntity.Transform.WorldToLocal(splinePoints[i]);
        //    }
        //}

        //private void CreateSplinePoints(Vector3[] splinePoints)
        //{
        //    ClearChildren(debugEntityOrbs);

        //    for (int i = 0; i < splinePoints.Length; i++)
        //    {
        //        var pointMesh = new LightPointMesh(GraphicsDevice);
        //        pointMesh.Build();

        //        var orb = new Entity()
        //        {
        //            new ModelComponent
        //            {
        //                Model = new Model
        //                {
        //                    GizmoUniformColorMaterial.Create(GraphicsDevice, Color.PeachPuff),
        //                    new Mesh { Draw = pointMesh.MeshDraw }
        //                },
        //                RenderGroup = RenderGroup,
        //            }
        //        };

        //        debugEntityOrbs.AddChild(orb);
        //        orb.Transform.Position = debugEntity.Transform.WorldToLocal(splinePoints[i]);
        //    }
        //}

        //private void DebugNodeLinks()
        //{
        //    var nodeLinkLineMesh = new LineMesh(GraphicsDevice);
        //    nodeLinkLineMesh.Build(new Vector3[2] { Component.Entity.Transform.Position, Component.Next.Entity.Transform.Position - Component.Entity.Transform.Position });
        //    debugEntityNodeLink.RemoveAll<ModelComponent>();
        //    debugEntityNodeLink.Add(
        //        new ModelComponent
        //        {
        //            Model = new Model
        //            {
        //                    GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green),
        //                    new Mesh { Draw = nodeLinkLineMesh.MeshDraw }
        //            },
        //            RenderGroup = RenderGroup
        //        }
        //    );
        //}

        //private void CreateOutHandler()
        //{
        //    var outMesh = new LightPointMesh(GraphicsDevice);
        //    outMesh.Build();

        //    debugEntityOut.Transform.Position = Component.TangentOut;
        //    debugEntityOut.Add(
        //        new ModelComponent
        //        {
        //            Model = new Model
        //            {
        //                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightSeaGreen),
        //                        new Mesh { Draw = outMesh.MeshDraw }
        //            },
        //            RenderGroup = RenderGroup
        //        }
        //    );
        //}

        //private void CreateInHandler()
        //{
        //    var inMesh = new LightPointMesh(GraphicsDevice);
        //    inMesh.Build();

        //    debugEntityIn.Transform.Position = Component.TangentIn;
        //    debugEntityIn.Add(
        //        new ModelComponent
        //        {
        //            Model = new Model
        //            {
        //                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.DeepPink),
        //                        new Mesh { Draw = inMesh.MeshDraw }
        //            },
        //            RenderGroup = RenderGroup
        //        }
        //    );
        //}

        //class LineMesh
        //{
        //    public MeshDraw MeshDraw;

        //    private Buffer vertexBuffer;

        //    private readonly GraphicsDevice graphicsDevice;

        //    public LineMesh(GraphicsDevice graphicsDevice)
        //    {
        //        this.graphicsDevice = graphicsDevice;
        //    }

        //    public void Build(Vector3[] positions)
        //    {
        //        var indices = new int[12 * 2];
        //        var vertices = new VertexPositionNormalTexture[positions.Length];

        //        for (int i = 0; i < positions.Length; i++)
        //        {
        //            vertices[0] = new VertexPositionNormalTexture(positions[i], Vector3.UnitY, Vector2.Zero);
        //        }

        //        int indexOffset = 0;
        //        // Top sides
        //        for (int i = 0; i < 4; i++)
        //        {
        //            indices[indexOffset++] = i;
        //            indices[indexOffset++] = (i + 1) % 4;
        //        }

        //        vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
        //        MeshDraw = new MeshDraw
        //        {
        //            PrimitiveType = PrimitiveType.LineList,
        //            DrawCount = indices.Length,
        //            IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
        //            VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
        //        };
        //    }
        //}

        //class LightPointMesh
        //{
        //    private const int Tesselation = 360 / 6;

        //    public MeshDraw MeshDraw;

        //    private Buffer vertexBuffer;

        //    private readonly GraphicsDevice graphicsDevice;

        //    public LightPointMesh(GraphicsDevice graphicsDevice)
        //    {
        //        this.graphicsDevice = graphicsDevice;
        //    }

        //    public void Build()
        //    {
        //        var indices = new int[2 * Tesselation * 3];
        //        var vertices = new VertexPositionNormalTexture[(Tesselation + 1) * 3];

        //        int indexCount = 0;
        //        int vertexCount = 0;
        //        // the two rings
        //        for (int j = 0; j < 3; j++)
        //        {
        //            var rotation = Matrix.Identity;
        //            if (j == 1)
        //            {
        //                rotation = Matrix.RotationX((float)Math.PI / 2);
        //            }
        //            else if (j == 2)
        //            {
        //                rotation = Matrix.RotationY((float)Math.PI / 2);
        //            }

        //            for (int i = 0; i <= Tesselation; i++)
        //            {
        //                var longitude = (float)(i * 2.0 * Math.PI / Tesselation);
        //                var dx = (float)Math.Cos(longitude);
        //                var dy = (float)Math.Sin(longitude);
        //                var range = 0.4f;
        //                var normal = new Vector3(dx * range, dy * range, 0);
        //                Vector3.TransformNormal(ref normal, ref rotation, out normal);

        //                if (i < Tesselation)
        //                {
        //                    indices[indexCount++] = vertexCount;
        //                    indices[indexCount++] = vertexCount + 1;
        //                }

        //                vertices[vertexCount++] = new VertexPositionNormalTexture(normal, normal, new Vector2(0));
        //            }
        //        }

        //        vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
        //        MeshDraw = new MeshDraw
        //        {
        //            PrimitiveType = PrimitiveType.LineList,
        //            DrawCount = indices.Length,
        //            IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
        //            VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
        //        };
        //    }
        //}

        //private void ClearChildren(Entity entity)
        //{
        //    var children = entity.GetChildren();
        //    foreach (var child in children)
        //    {
        //        entity.RemoveChild(child);
        //    }
        //}

    //}
}
