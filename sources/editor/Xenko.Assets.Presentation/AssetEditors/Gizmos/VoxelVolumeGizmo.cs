// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Voxels;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo to display the bounding boxes for voxel volumes inside the editor as a gizmo. 
    /// this gizmo uses volume scale as the extent of the bounding box and is not affected by rotation
    /// </summary>
    [GizmoComponent(typeof(VoxelVolumeComponent), false)]
    public class VoxelVolumeGizmo : EntityGizmo<VoxelVolumeComponent>
    {
        private BoxMesh box;
        private Material material;
        private Entity debugRootEntity;
        private Entity debugEntity;

        public VoxelVolumeGizmo(EntityComponent component) : base(component)
        {
        }

        protected override Entity Create()
        {
            debugRootEntity = new Entity($"Voxel volume of {Component.Entity.Name}");

            material = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.CornflowerBlue);

            box = new BoxMesh(GraphicsDevice);
            box.Build();

            debugEntity = new Entity($"Voxel volume mesh of {Component.Entity.Name}")
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        material,
                        new Mesh { Draw = box.MeshDraw },
                    },
                    RenderGroup = RenderGroup,
                }
            };

            return debugRootEntity;
        }

        public override void Update()
        {
            if (ContentEntity == null || GizmoRootEntity == null)
                return;

            // calculate the world matrix of the gizmo so that it is positioned exactly as the corresponding scene entity
            // except the scale that is re-adjusted to the gizmo desired size (gizmo are insert at scene root so LocalMatrix = WorldMatrix)
            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rotation, out translation);

            // Translation and Scale but no rotation on bounding boxes
            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = new Vector3(Component.VoxelVolumeSize * 0.5f);
            GizmoRootEntity.Transform.UpdateWorldMatrix();
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
                        GizmoRootEntity.AddChild(debugEntity);
                    else
                        GizmoRootEntity.RemoveChild(debugEntity);
                }
            }
        }

        class BoxMesh
        {
            public MeshDraw MeshDraw;

            private Buffer vertexBuffer;

            private readonly GraphicsDevice graphicsDevice;

            public BoxMesh(GraphicsDevice graphicsDevice)
            {
                this.graphicsDevice = graphicsDevice;
            }

            public void Build()
            {
                var indices = new int[12 * 2];
                var vertices = new VertexPositionNormalTexture[8];

                vertices[0] = new VertexPositionNormalTexture(new Vector3(-1, 1, -1), Vector3.UnitY, Vector2.Zero);
                vertices[1] = new VertexPositionNormalTexture(new Vector3(-1, 1, 1), Vector3.UnitY, Vector2.Zero);
                vertices[2] = new VertexPositionNormalTexture(new Vector3(1, 1, 1), Vector3.UnitY, Vector2.Zero);
                vertices[3] = new VertexPositionNormalTexture(new Vector3(1, 1, -1), Vector3.UnitY, Vector2.Zero);

                int indexOffset = 0;
                // Top sides
                for (int i = 0; i < 4; i++)
                {
                    indices[indexOffset++] = i;
                    indices[indexOffset++] = (i + 1) % 4;
                }

                // Duplicate vertices and indices to bottom part
                for (int i = 0; i < 4; i++)
                {
                    vertices[i + 4] = vertices[i];
                    vertices[i + 4].Position.Y = -vertices[i + 4].Position.Y;

                    indices[indexOffset++] = indices[i * 2] + 4;
                    indices[indexOffset++] = indices[i * 2 + 1] + 4;
                }

                // Sides
                for (int i = 0; i < 4; i++)
                {
                    indices[indexOffset++] = i;
                    indices[indexOffset++] = i + 4;
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
