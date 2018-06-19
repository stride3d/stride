// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.Data;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Rendering.ProceduralModels
{
    /// <summary>
    /// Base class for primitive procedural model.
    /// </summary>
    [DataContract]
    public abstract class PrimitiveProceduralModelBase : IProceduralModel
    {
        protected PrimitiveProceduralModelBase()
        {
            MaterialInstance = new MaterialInstance();
            UvScale = Vector2.One;
        }

        public void SetMaterial(string name, Material material)
        {
            if (name == "Material")
            {
                MaterialInstance.Material = material;
            }
        }

        [DataMember(510)]
        public Vector3 Scale = Vector3.One;

        /// <summary>
        /// Gets or sets the UV scale.
        /// </summary>
        /// <value>The UV scale</value>
        /// <userdoc>The scale to apply to the UV coordinates of the shape. This can be used to tile a texture on it.</userdoc>
        [DataMember(520)]
        [Display("UV Scale")]
        public Vector2 UvScale { get; set; }

        /// <summary>
        /// Gets or sets the local offset that will be applied to the procedural model's vertexes.
        /// </summary>
        [DataMember(530)]
        public Vector3 LocalOffset { get; set; }

        /// <summary>
        /// Gets the material instance.
        /// </summary>
        /// <value>The material instance.</value>
        /// <userdoc>The reference material asset to use with this model.</userdoc>
        [DataMember(600)]
        [NotNull]
        [Display("Material")]
        public MaterialInstance MaterialInstance { get; private set; }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public IEnumerable<KeyValuePair<string, MaterialInstance>> MaterialInstances { get { yield return new KeyValuePair<string, MaterialInstance>("Material", MaterialInstance); } }

        public void Generate(IServiceRegistry services, Model model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var needsTempDevice = false;
            var graphicsDevice = services?.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
            if (graphicsDevice == null)
            {
                graphicsDevice = GraphicsDevice.New();
                needsTempDevice = true;
            }

            var data = CreatePrimitiveMeshData();

            if (data.Vertices.Length == 0)
            {
                throw new InvalidOperationException("Invalid GeometricPrimitive [{0}]. Expecting non-zero Vertices array");
            }

            if (LocalOffset != Vector3.Zero)
            {
                for (var index = 0; index < data.Vertices.Length; index++)
                {
                    data.Vertices[index].Position += LocalOffset;
                }
            }

            //Scale if necessary
            if (Scale != Vector3.One)
            {
                var inverseMatrix = Matrix.Scaling(Scale);
                inverseMatrix.Invert();
                for (var index = 0; index < data.Vertices.Length; index++)
                {
                    data.Vertices[index].Position *= Scale;
                    Vector3.TransformCoordinate(ref data.Vertices[index].Normal, ref inverseMatrix, out data.Vertices[index].Normal);
                }
            }

            var boundingBox = BoundingBox.Empty;
            for (int i = 0; i < data.Vertices.Length; i++)
                BoundingBox.Merge(ref boundingBox, ref data.Vertices[i].Position, out boundingBox);

            BoundingSphere boundingSphere;
            unsafe
            {
                fixed (void* verticesPtr = data.Vertices)
                    BoundingSphere.FromPoints((IntPtr)verticesPtr, 0, data.Vertices.Length, VertexPositionNormalTexture.Size, out boundingSphere);
            }

            var originalLayout = data.Vertices[0].GetLayout();

            // Generate Tangent/BiNormal vectors
            var resultWithTangentBiNormal = VertexHelper.GenerateTangentBinormal(originalLayout, data.Vertices, data.Indices);

            // Generate Multitexcoords
            var result = VertexHelper.GenerateMultiTextureCoordinates(resultWithTangentBiNormal);

            var meshDraw = new MeshDraw();

            var layout = result.Layout;
            var vertexBuffer = result.VertexBuffer;
            var indices = data.Indices;

            if (indices.Length < 0xFFFF)
            {
                var indicesShort = new ushort[indices.Length];
                for (int i = 0; i < indicesShort.Length; i++)
                {
                    indicesShort[i] = (ushort)indices[i];
                }
                meshDraw.IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indicesShort).RecreateWith(indicesShort), false, indices.Length);
                if (needsTempDevice)
                {
                    var indexData = BufferData.New(BufferFlags.IndexBuffer, indicesShort);
                    meshDraw.IndexBuffer = new IndexBufferBinding(indexData.ToSerializableVersion(), false, indices.Length);
                }
            }
            else
            {
                if (graphicsDevice.Features.CurrentProfile <= GraphicsProfile.Level_9_3)
                {
                    throw new InvalidOperationException("Cannot generate more than 65535 indices on feature level HW <= 9.3");
                }

                meshDraw.IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices).RecreateWith(indices), true, indices.Length);
                if (needsTempDevice)
                {
                    var indexData = BufferData.New(BufferFlags.IndexBuffer, indices);
                    meshDraw.IndexBuffer = new IndexBufferBinding(indexData.ToSerializableVersion(), true, indices.Length);
                }
            }

            meshDraw.VertexBuffers = new[] { new VertexBufferBinding(Buffer.New(graphicsDevice, vertexBuffer, BufferFlags.VertexBuffer).RecreateWith(vertexBuffer), layout, data.Vertices.Length) };
            if (needsTempDevice)
            {
                var vertexData = BufferData.New(BufferFlags.VertexBuffer, vertexBuffer);
                meshDraw.VertexBuffers = new[] { new VertexBufferBinding(vertexData.ToSerializableVersion(), layout, data.Vertices.Length) };
            }

            meshDraw.DrawCount = indices.Length;
            meshDraw.PrimitiveType = PrimitiveType.TriangleList;

            var mesh = new Mesh { Draw = meshDraw, BoundingBox = boundingBox, BoundingSphere = boundingSphere };

            model.BoundingBox = boundingBox;
            model.BoundingSphere = boundingSphere;
            model.Add(mesh);

            if (MaterialInstance?.Material != null)
            {
                model.Materials.Add(MaterialInstance);
            }

            if (needsTempDevice)
            {
                graphicsDevice.Dispose();
            }
        }

        protected abstract GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData();
    }
}
