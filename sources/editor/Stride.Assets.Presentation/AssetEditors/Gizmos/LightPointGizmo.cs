// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Buffer = Stride.Graphics.Buffer;
using Material = Stride.Rendering.Material;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// The gizmo for the ambient light component
    /// </summary>
    public class LightPointGizmo : LightGizmo
    {
        private Entity pointEntity;

        private LightPointMesh pointMesh;

        private Material pointMaterial;

        private LightPoint LightPoint
        {
            get
            {
                var lightSpot = LightComponent.Type as LightPoint;
                return lightSpot ?? new LightPoint();
            }
        }

        public LightPointGizmo(EntityComponent component)
            : base(component, "Point", GizmoResources.PointLightGizmo)
        {
        }

        protected override Entity Create()
        {
            var root = base.Create();

            pointMesh= new LightPointMesh(GraphicsDevice);
            pointMesh.Build();

            pointMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, (Color)new Color4(GetLightColor(GraphicsDevice), 1f));

            pointEntity = new Entity("Point Mesh of {0}".ToFormat(root.Id))
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        pointMaterial,
                        new Mesh { Draw = pointMesh.MeshDraw },
                    },
                    RenderGroup = RenderGroup,
                }
            };

            return root;
        }

        public override void Update()
        {
            base.Update();

            // update pointEntity aspect
            pointEntity.Transform.Scale = new Vector3(LightPoint.Radius);

            // update the spot color
            GizmoUniformColorMaterial.UpdateColor(GraphicsDevice, pointMaterial, (Color)new Color4(GetLightColor(GraphicsDevice), 1f));
        }
        
        public override bool IsSelected
        {
            set
            {
                bool hasChanged = IsSelected != value;
                base.IsSelected = value;

                if (hasChanged)
                {
                    if (IsSelected)
                        GizmoRootEntity.AddChild(pointEntity);
                    else
                        GizmoRootEntity.RemoveChild(pointEntity);
                }
            }
        }

        class LightPointMesh
        {
            private const int Tesselation = 360/6;

            public MeshDraw MeshDraw;

            private Buffer vertexBuffer;

            private readonly GraphicsDevice graphicsDevice;

            public LightPointMesh(GraphicsDevice graphicsDevice)
            {
                this.graphicsDevice = graphicsDevice;
            }

            public void Build()
            {
                var indices = new int[2 * Tesselation  * 3];
                var vertices = new VertexPositionNormalTexture[(Tesselation + 1) * 3];

                int indexCount = 0;
                int vertexCount = 0;
                // the two rings
                for (int j = 0; j < 3; j++)
                {
                    var rotation = Matrix.Identity;
                    if (j == 1)
                    {
                        rotation = Matrix.RotationX((float)Math.PI / 2);
                    }
                    else if (j == 2)
                    {
                        rotation = Matrix.RotationY((float)Math.PI / 2);
                    }

                    for (int i = 0; i <= Tesselation; i++)
                    {
                        var longitude = (float)(i * 2.0 * Math.PI / Tesselation);
                        var dx = (float)Math.Cos(longitude);
                        var dy = (float)Math.Sin(longitude);

                        var normal = new Vector3(dx, dy, 0);
                        Vector3.TransformNormal(ref normal, ref rotation, out normal);

                        if (i < Tesselation)
                        {
                            indices[indexCount++] = vertexCount;
                            indices[indexCount++] = vertexCount + 1;
                        }

                        vertices[vertexCount++] = new VertexPositionNormalTexture(normal, normal, new Vector2(0));
                    }
                }

                vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
                MeshDraw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.LineList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
                };
            }
        }
    }
}
