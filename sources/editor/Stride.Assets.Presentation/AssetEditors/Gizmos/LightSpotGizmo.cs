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
    public class LightSpotGizmo : LightGizmo
    {
        private Entity spotEntity;

        private LightSpotMesh spotMesh;

        private Material spotMaterial;

        private float currentAngleOuterInRadians;
        private float currentRange;
        private float currentAspectRatio;
        private float currentProjectionPlaneDistance;
        private Texture currentProjectiveTexture;

        private LightSpot LightSpot
        {
            get
            {
                var lightSpot = LightComponent.Type as LightSpot;
                return lightSpot ?? new LightSpot();
            }
        }

        public LightSpotGizmo(EntityComponent component)
            : base(component, "Spot", GizmoResources.SpotLightGizmo)
        {
        }

        private void UpdateModelComponentMesh() // TODO: If this is being called multiple times, are the resources (vertex & index buffers) being freed properly?
        {
            ModelComponent modelComponent = spotEntity.Components.Get<ModelComponent>();
            modelComponent.Model.Meshes[0].Draw = spotMesh.MeshDraw;    // The spot light gizmo should always have only one mesh, so we directly access index 0.
        }

        protected override Entity Create()
        {
            var root = base.Create();

            spotMesh = new LightSpotMesh(GraphicsDevice);
            spotMesh.Build(GraphicsCommandList, LightSpot);

            spotMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, (Color)new Color4(GetLightColor(GraphicsDevice), 1f));

            spotEntity = new Entity("Spot Mesh of {0}".ToFormat(root.Id))
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        spotMaterial,
                        new Mesh { Draw = spotMesh.MeshDraw },
                    },
                    RenderGroup = RenderGroup,
                }
            };

            return root;
        }

        public override void Update()
        {
            base.Update();

            // Update the frustum mesh to match the changed light settings:
            bool lightingModeChanged = (LightSpot.ProjectiveTexture == null && currentProjectiveTexture != null) || // <- if texture removed
                                       (LightSpot.ProjectiveTexture != null && currentProjectiveTexture == null);   // <- if texture added

            // Check if we need to rebuild the whole mesh:
            if (lightingModeChanged)
            {
                // Rebuild the mesh because we switched between textured and untextured mode:
                spotMesh.Build(GraphicsCommandList, LightSpot); // TODO: Does "Build()" properly release the resources?
                // Now assign the new mesh to the model component:
                UpdateModelComponentMesh();
            }

            // Check if we only need to update the vertex positions:
            bool GeometryAffectingParametersChanged = !MathUtil.NearEqual(currentAngleOuterInRadians, LightSpot.AngleOuterInRadians) ||
                                                      !MathUtil.NearEqual(currentRange, LightSpot.Range) ||
                                                      !MathUtil.NearEqual(currentAspectRatio, LightSpot.AspectRatio) || // TODO: This will trigger a rebuild even if no texture is being used.
                                                      !MathUtil.NearEqual(currentProjectionPlaneDistance, LightSpot.ProjectionPlaneDistance) || // TODO: This will trigger a rebuild even if no texture is being used.
                                                      lightingModeChanged;

            if (GeometryAffectingParametersChanged) // TODO: PERFORMANCE: We're doing redundant work. If the lighting mode changes, we do a rebuild and then we also update the vertex positions (redundant).
            {
                // Save the current state:
                currentAngleOuterInRadians = LightSpot.AngleOuterInRadians; // "AngleOuterInRadians" already contains the max value between "AngleInner" and "AngleOuter".
                currentRange = LightSpot.Range;
                currentAspectRatio = LightSpot.AspectRatio;
                currentProjectionPlaneDistance = LightSpot.ProjectionPlaneDistance;
                currentProjectiveTexture = LightSpot.ProjectiveTexture;

                // Rebuild the mesh based on the new parameters:
                spotMesh.Rebuild(GraphicsCommandList, LightSpot);
            }

            // update the spot color
            GizmoUniformColorMaterial.UpdateColor(GraphicsDevice, spotMaterial, (Color)new Color4(GetLightColor(GraphicsDevice), 1f));
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
                        GizmoRootEntity.AddChild(spotEntity);
                    else
                        GizmoRootEntity.RemoveChild(spotEntity);
                }
            }
        }

        class LightSpotMesh
        {
            private const int Tesselation = 20;

            public MeshDraw MeshDraw;

            private Buffer vertexBuffer;

            private readonly GraphicsDevice graphicsDevice;

            public LightSpotMesh(GraphicsDevice graphicsDevice)
            {
                this.graphicsDevice = graphicsDevice;
            }

            public void Build(CommandList commandList, LightSpot lightSpot)
            {
                int[] indices;

                if (lightSpot.ProjectiveTexture != null)    // If no projection texture has been supplied, we render the regular cone:
                {
                    indices = BuildRectangleIndexBuffer();
                    vertexBuffer = Buffer.Vertex.New(graphicsDevice, new VertexPositionNormalTexture[9], GraphicsResourceUsage.Dynamic);
                    RebuildRectangleVertexBuffer(commandList, lightSpot);
                }
                else    // If a projection texture has been supplied, we render a rectangular frustum instead:
                {
                    indices = BuildConeIndexBuffer();
                    vertexBuffer = Buffer.Vertex.New(graphicsDevice, new VertexPositionNormalTexture[4 * Tesselation + 1], GraphicsResourceUsage.Dynamic);
                    RebuildConeVertexBuffer(commandList, lightSpot);
                }

                MeshDraw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.LineList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
                };
            }

            public void Rebuild(CommandList commandList, LightSpot lightSpot)
            {
                if (lightSpot.ProjectiveTexture != null)
                {
                    RebuildRectangleVertexBuffer(commandList, lightSpot);
                }
                else
                {
                    RebuildConeVertexBuffer(commandList, lightSpot);   // TODO (?): This ignores the aspect ratio.
                }
            }

            private unsafe void RebuildConeVertexBuffer(CommandList commandList, LightSpot lightSpot)
            {
                var mappedVertices = commandList.MapSubresource(vertexBuffer, 0, MapMode.WriteDiscard);
                var vertexPointer = mappedVertices.DataBox.DataPointer;
                var vertex = (VertexPositionNormalTexture*)vertexPointer;

                // the two ring
                var angleOuter = Math.Max(lightSpot.AngleInner, lightSpot.AngleOuter);
                for (int i = 0; i < 4 * Tesselation; i++)
                {
                    var z = -lightSpot.Range;
                    var theta = 2 * i * MathUtil.Pi / (4 * Tesselation);
                    var radiusBeam = Math.Abs(z) * (float)Math.Tan(MathUtil.DegreesToRadians(angleOuter / 2));

                    vertex[i].Position = new Vector3(radiusBeam * (float)Math.Cos(theta), radiusBeam * (float)Math.Sin(theta), z);
                    vertex[i].Normal = new Vector3(0, 0, -1);
                }

                // the origin point
                vertex[4 * Tesselation].Position = Vector3.Zero;
                vertex[4 * Tesselation].Normal = new Vector3(0, 0, -1);

                commandList.UnmapSubresource(mappedVertices);
            }

            private unsafe void RebuildRectangleVertexBuffer(CommandList commandList, LightSpot lightSpot)
            {
                var mappedVertices = commandList.MapSubresource(vertexBuffer, 0, MapMode.WriteDiscard);
                var vertexPointer = mappedVertices.DataBox.DataPointer;
                var vertex = (VertexPositionNormalTexture*)vertexPointer;

                Vector3 normal = new Vector3(0, 0, -1);

                // The origin point:
                vertex[0].Position = Vector3.Zero;
                vertex[0].Normal = normal;

                // The four corners at the end:
                float angleOuterInRadians = MathUtil.DegreesToRadians(Math.Max(lightSpot.AngleInner, lightSpot.AngleOuter));
                float y = (float)Math.Tan(angleOuterInRadians / 2.0f) * lightSpot.Range;  // TODO: Is this correct?
                //float y = (float)Math.Tan(lightSpot.AngleOuter) * lightSpot.Range * 2.0f;  // TODO: Is this correct?
                float x = y * lightSpot.AspectRatio;  // TODO: Is this correct?

                vertex[1].Position = new Vector3(-x, -y, -lightSpot.Range);
                vertex[1].Normal = normal;

                vertex[2].Position = new Vector3(x, -y, -lightSpot.Range);
                vertex[2].Normal = normal;

                vertex[3].Position = new Vector3(x, y, -lightSpot.Range);
                vertex[3].Normal = normal;

                vertex[4].Position = new Vector3(-x, y, -lightSpot.Range);
                vertex[4].Normal = normal;

                // Projection plane corners:
                float projectionPlaneDistance = Math.Min(lightSpot.ProjectionPlaneDistance, lightSpot.Range);

                float projectionPlaneX = x / lightSpot.Range * projectionPlaneDistance;
                float projectionPlaneY = y / lightSpot.Range * projectionPlaneDistance;

                vertex[5].Position = new Vector3(-projectionPlaneX, -projectionPlaneY, -projectionPlaneDistance);
                vertex[5].Normal = normal;

                vertex[6].Position = new Vector3(projectionPlaneX, -projectionPlaneY, -projectionPlaneDistance);
                vertex[6].Normal = normal;

                vertex[7].Position = new Vector3(projectionPlaneX, projectionPlaneY, -projectionPlaneDistance);
                vertex[7].Normal = normal;

                vertex[8].Position = new Vector3(-projectionPlaneX, projectionPlaneY, -projectionPlaneDistance);
                vertex[8].Normal = normal;

                commandList.UnmapSubresource(mappedVertices);
            }

            private static int[] BuildConeIndexBuffer()
            {
                int[] indices = new int[2 * (4 * Tesselation + 4)];

                // the two rings
                for (int i = 0; i < 4 * Tesselation - 1; i++)
                {
                    indices[2 * i + 0] = i;
                    indices[2 * i + 1] = i + 1;
                }
                // the last lines of the two rings
                indices[8 * Tesselation - 2] = 4 * Tesselation - 1;
                indices[8 * Tesselation - 1] = 0;

                // the cone edges
                indices[8 * Tesselation + 0] = 4 * Tesselation;
                indices[8 * Tesselation + 1] = 0 * Tesselation;

                indices[8 * Tesselation + 2] = 4 * Tesselation;
                indices[8 * Tesselation + 3] = 1 * Tesselation;

                indices[8 * Tesselation + 4] = 4 * Tesselation;
                indices[8 * Tesselation + 5] = 2 * Tesselation;

                indices[8 * Tesselation + 6] = 4 * Tesselation;
                indices[8 * Tesselation + 7] = 3 * Tesselation;

                return indices;
            }

            private static int[] BuildRectangleIndexBuffer()
            {
                int[] indices = new int[24];

                // Lines connecting the origin and each corner at the end:
                indices[0] = 0;
                indices[1] = 1;

                indices[2] = 0;
                indices[3] = 2;

                indices[4] = 0;
                indices[5] = 3;

                indices[6] = 0;
                indices[7] = 4;

                // Lines connecting all the corners at the end:
                indices[8] = 1;
                indices[9] = 2;

                indices[10] = 2;
                indices[11] = 3;

                indices[12] = 3;
                indices[13] = 4;

                indices[14] = 4;
                indices[15] = 1;

                // Lines showing the distance of the projection plane:
                indices[16] = 5;
                indices[17] = 6;

                indices[18] = 6;
                indices[19] = 7;

                indices[20] = 7;
                indices[21] = 8;

                indices[22] = 8;
                indices[23] = 5;

                return indices;
            }
        }
    }
}
