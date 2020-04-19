// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo for the camera component.
    /// </summary>
    [GizmoComponent(typeof(CameraComponent), true)]
    public class CameraGizmo : BillboardingGizmo<CameraComponent>
    {
        private CameraFrustumMesh frustumMesh;

        private struct CameraParameters
        {
            public readonly float AspectRatio;
            public readonly float VerticalFov;
            public readonly float NearPlane;
            public readonly float FarPlane;
            public readonly float OrthographicSize;
            public readonly CameraProjectionMode ProjectionMode;

            public CameraParameters(CameraComponent component, float defaultAspect)
                : this()
            {
                AspectRatio = component.UseCustomAspectRatio ? component.AspectRatio : defaultAspect;
                VerticalFov = component.VerticalFieldOfView;
                NearPlane = component.NearClipPlane;
                FarPlane = component.FarClipPlane;
                OrthographicSize = component.OrthographicSize;
                ProjectionMode = component.Projection;
            }
        }

        private CameraParameters cameraParameters;

        private Entity frustum;

        public CameraGizmo(EntityComponent component)
            : base(component, "Camera gizmo", GizmoResources.CameraGizmo)
        {
        }

        protected override Entity Create()
        {
            var root = base.Create();

            var renderingSettings = Game.PackageSettings?.GetConfiguration<RenderingSettings>();
            var aspect = (renderingSettings == null) ? 1.7778f : (float)renderingSettings.DefaultBackBufferWidth / (float)renderingSettings.DefaultBackBufferHeight;

            cameraParameters = new CameraParameters(Component, aspect);

            frustumMesh = new CameraFrustumMesh(GraphicsDevice);
            frustumMesh.Build(GraphicsCommandList, cameraParameters);

            var frustumMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, new Color(0.75f, 0.75f, 1f, 1f));

            frustum = new Entity("Camera frustumMesh of {0}".ToFormat(root.Id))
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        frustumMaterial,
                        new Mesh { Draw = frustumMesh.MeshDraw },
                    },
                    RenderGroup = RenderGroup,
                }
            };

            return root;
        }

        public override void Update()
        {
            base.Update();

            var renderingSettings = Game.PackageSettings?.GetConfiguration<RenderingSettings>();
            var aspect = (renderingSettings == null) ? 1.7778f : (float)renderingSettings.DefaultBackBufferWidth / (float)renderingSettings.DefaultBackBufferHeight;

            // update frustumMesh aspect
            var newCameraParameters = new CameraParameters(Component, aspect);
            if (!newCameraParameters.Equals(cameraParameters))
            {
                cameraParameters = newCameraParameters;
                frustumMesh.RebuildVertexBuffer(GraphicsCommandList, cameraParameters);
            }
            
            // update frustumMesh transformation
            frustum.Transform.UseTRS = false;
            frustum.Transform.UpdateWorldMatrix();
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
                    {
                        GizmoRootEntity.AddChild(frustum);
                    }
                    else
                    {
                        GizmoRootEntity.RemoveChild(frustum);
                    }
                }
            }
        }

        class CameraFrustumMesh
        {
            public MeshDraw MeshDraw;

            private Buffer vertexBuffer;

            private readonly GraphicsDevice graphicsDevice;

            public CameraFrustumMesh(GraphicsDevice graphicsDevice)
            {
                this.graphicsDevice = graphicsDevice;
            }

            public void Build(CommandList commandList, CameraParameters parameters)
            {
                var indices = new []
                {
                    0, 1, 
                    1, 2, 
                    2, 3, 
                    3, 0, 

                    4, 5,
                    5, 6,
                    6, 7,
                    7, 4,

                    0, 4,
                    1, 5,
                    2, 6,
                    3, 7
                };

                vertexBuffer = Buffer.Vertex.New(graphicsDevice, new VertexPositionNormalTexture[8], GraphicsResourceUsage.Dynamic);
                RebuildVertexBuffer(commandList, parameters);

                MeshDraw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.LineList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
                };
            }

            public unsafe void RebuildVertexBuffer(CommandList commandList, CameraParameters parameters)
            {
                var mappedVertices = commandList.MapSubresource(vertexBuffer, 0, MapMode.WriteDiscard);
                var vertexPointer = mappedVertices.DataBox.DataPointer;

                var vertex = (VertexPositionNormalTexture*)vertexPointer;
                var offsets = new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1) };
                for (int i = 0; i < 8; i++)
                {
                    var index = i % 4;
                    var zDist = i < 4 ? parameters.NearPlane : parameters.FarPlane;

                    var y = parameters.ProjectionMode == CameraProjectionMode.Perspective
                        ? zDist * (float)Math.Tan(MathUtil.DegreesToRadians(parameters.VerticalFov) / 2)
                        : parameters.OrthographicSize / 2f;

                    vertex->Position = new Vector3(new Vector2(parameters.AspectRatio*y, y) * offsets[index], -zDist);

                    vertex->Normal = new Vector3(0, 0, -1);

                    ++vertex;
                }

                commandList.UnmapSubresource(mappedVertices);
            }
        }
    }
}
