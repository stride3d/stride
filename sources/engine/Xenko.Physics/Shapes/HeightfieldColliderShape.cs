// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Threading;
using Xenko.Graphics;
using Xenko.Rendering;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Physics.Shapes
{
    public class HeightfieldColliderShape : ColliderShape
    {
        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<short> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
            : this(heightStickWidth, heightStickLength, HeightfieldTypes.Short, dynamicFieldData, heightScale, minHeight, maxHeight, flipQuadEdges)
        {
        }

        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<byte> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
            : this(heightStickWidth, heightStickLength, HeightfieldTypes.Byte, dynamicFieldData, heightScale, minHeight, maxHeight, flipQuadEdges)
        {
        }

        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<float> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
            : this(heightStickWidth, heightStickLength, HeightfieldTypes.Float, dynamicFieldData, heightScale, minHeight, maxHeight, flipQuadEdges)
        {
        }

        internal HeightfieldColliderShape
        (
            int heightStickWidth,
            int heightStickLength,
            HeightfieldTypes heightType,
            object dynamicFieldData,
            float heightScale,
            float minHeight,
            float maxHeight,
            bool flipQuadEdges
        )
        {
            Type = ColliderShapeTypes.Heightfield;
            Is2D = false;

            HeightStickWidth = heightStickWidth;
            HeightStickLength = heightStickLength;
            HeightType = heightType;
            HeightScale = heightScale;
            MinHeight = minHeight;
            MaxHeight = maxHeight;

            cachedScaling = Vector3.One;

            switch (HeightType)
            {
                case HeightfieldTypes.Short:
                    ShortArray = dynamicFieldData as UnmanagedArray<short>;

                    InternalShape = new BulletSharp.HeightfieldShape(HeightStickWidth, HeightStickLength, ShortArray.Pointer, HeightScale, MinHeight, MaxHeight, 1, (int)BulletPhyScalarType.PhyShort, flipQuadEdges)
                    {
                        LocalScaling = cachedScaling,
                    };

                    break;

                case HeightfieldTypes.Byte:
                    ByteArray = dynamicFieldData as UnmanagedArray<byte>;

                    InternalShape = new BulletSharp.HeightfieldShape(HeightStickWidth, HeightStickLength, ByteArray.Pointer, HeightScale, MinHeight, MaxHeight, 1, (int)BulletPhyScalarType.PhyUchar, flipQuadEdges)
                    {
                        LocalScaling = cachedScaling,
                    };

                    break;

                case HeightfieldTypes.Float:
                    FloatArray = dynamicFieldData as UnmanagedArray<float>;

                    InternalShape = new BulletSharp.HeightfieldShape(HeightStickWidth, HeightStickLength, FloatArray.Pointer, HeightScale, MinHeight, MaxHeight, 1, (int)BulletPhyScalarType.PhyFloat, flipQuadEdges)
                    {
                        LocalScaling = cachedScaling,
                    };

                    break;
            }

            var halfRange = (MaxHeight - MinHeight) * 0.5f;
            var offset = -(MinHeight + halfRange);
            DebugPrimitiveMatrix = Matrix.Translation(new Vector3(0, offset, 0)) * Matrix.Scaling(Vector3.One * DebugScaling);
        }

        public bool UseDiamondSubdivision
        {
            set { ((BulletSharp.HeightfieldShape)InternalShape).SetUseDiamondSubdivision(value); }
        }

        public bool UseZigzagSubdivision
        {
            set { ((BulletSharp.HeightfieldShape)InternalShape).SetUseZigzagSubdivision(value); }
        }

        public UnmanagedArray<short> ShortArray { get; private set; }

        public UnmanagedArray<byte> ByteArray { get; private set; }

        public UnmanagedArray<float> FloatArray { get; private set; }

        public int HeightStickWidth { get; private set; }
        public int HeightStickLength { get; private set; }
        public HeightfieldTypes HeightType { get; private set; }
        public float HeightScale { get; private set; }
        public float MinHeight { get; private set; }
        public float MaxHeight { get; private set; }

        private readonly int MaxTileWidth = 64;
        private readonly int MaxTileHeight = 64;

        public override IDebugPrimitive CreateUpdatableDebugPrimitive(GraphicsDevice graphicsDevice)
        {
            var width = HeightStickWidth - 1;
            var height = HeightStickLength - 1;

            var debugPrimitive = new HeightfieldDebugPrimitive();

            var offset = new Vector3(-(width * 0.5f), 0, -(height * 0.5f));

            for (int j = 0; j < height; j += MaxTileHeight)
            {
                for (int i = 0; i < width; i += MaxTileWidth)
                {
                    var tileWidth = Math.Min(MaxTileWidth, width - i);
                    var tileHeight = Math.Min(MaxTileHeight, height - j);

                    var point = new Point(i, j);

                    CreateTileMeshData(point, tileWidth, tileHeight, offset + new Vector3(i, 0, j), out var vertices, out var indices);

                    var tile = new HeightfieldDebugPrimitiveTile
                    {
                        Point = point,
                        Width = tileWidth,
                        Height = tileHeight,
                        Vertices = vertices,
                        MeshDraw = CreateTileMeshDraw(graphicsDevice, vertices, indices),
                    };

                    debugPrimitive.Tiles.Add(tile);
                }
            }

            return debugPrimitive;
        }

        public override void UpdateDebugPrimitive(CommandList commandList, IDebugPrimitive debugPrimitive)
        {
            var heightfield = debugPrimitive as HeightfieldDebugPrimitive;

            if (heightfield == null)
            {
                return;
            }

            Dispatcher.ForEach(heightfield.Tiles, (tile) =>
            {
                for (int j = 0; j <= tile.Height; ++j)
                {
                    for (int i = 0; i <= tile.Width; ++i)
                    {
                        tile.Vertices[j * (tile.Width + 1) + i].Position.Y = GetHeight(tile.Point.X + i, tile.Point.Y + j);
                    }
                }
            });

            foreach (var tile in heightfield.Tiles)
            {
                tile.MeshDraw.VertexBuffers[0].Buffer.SetData(commandList, tile.Vertices);
            }
        }

        private float GetHeight(int x, int y)
        {
            var index = y * HeightStickWidth + x;

            switch (HeightType)
            {
                case HeightfieldTypes.Short:
                    return ShortArray[index] * HeightScale;

                case HeightfieldTypes.Byte:
                    return ByteArray[index] * HeightScale;

                case HeightfieldTypes.Float:
                    return FloatArray[index];

                default:
                    throw new NotSupportedException();
            }
        }

        private MeshDraw CreateTileMeshDraw(GraphicsDevice graphicsDevice, VertexPositionNormalTexture[] vertices, ushort[] indices)
        {
            var vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices, GraphicsResourceUsage.Dynamic).RecreateWith(vertices);
            var indexBuffer = Buffer.Index.New(graphicsDevice, indices).RecreateWith(indices);

            var meshDraw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.TriangleList,
                VertexBuffers = new VertexBufferBinding[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
                IndexBuffer = new IndexBufferBinding(indexBuffer, false, indexBuffer.ElementCount),
                StartLocation = 0,
                DrawCount = indexBuffer.ElementCount,
            };

            return meshDraw;
        }

        private void CreateTileMeshData(Point point, int width, int height, Vector3 offset, out VertexPositionNormalTexture[] vertices, out ushort[] indices)
        {
            vertices = new VertexPositionNormalTexture[(width + 1) * (height + 1)];

            ushort GetIndex(int x, int y) => (ushort)(y * (width + 1) + x);

            var stepU = 1f / width;
            var stepV = 1f / height;

            for (int j = 0; j <= height; ++j)
            {
                for (int i = 0; i <= width; ++i)
                {
                    vertices[GetIndex(i, j)] = new VertexPositionNormalTexture(offset + new Vector3(i, GetHeight(point.X + i, point.Y + j), j), Vector3.UnitY, new Vector2(stepU * i, stepV * j));
                }
            }

            indices = new ushort[width * height * 6];
            var count = 0;
            for (int j = 0; j < height; ++j)
            {
                for (int i = 0; i < width; ++i)
                {
                    indices[count++] = GetIndex(i, j);
                    indices[count++] = GetIndex(i + 1, j);
                    indices[count++] = GetIndex(i, j + 1);

                    indices[count++] = GetIndex(i + 1, j);
                    indices[count++] = GetIndex(i + 1, j + 1);
                    indices[count++] = GetIndex(i, j + 1);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            ShortArray?.Dispose();
            ShortArray = null;
            ByteArray?.Dispose();
            ByteArray = null;
            FloatArray?.Dispose();
            FloatArray = null;
        }

        private enum BulletPhyScalarType
        {
            PhyFloat,
            PhyDouble,
            PhyInteger,
            PhyShort,
            PhyFixedpoint88,
            PhyUchar,
        }

        public class HeightfieldDebugPrimitiveTile
        {
            public Point Point;
            public int Width;
            public int Height;
            public VertexPositionNormalTexture[] Vertices;
            public MeshDraw MeshDraw;
        }

        public class HeightfieldDebugPrimitive : IDebugPrimitive
        {
            public readonly List<HeightfieldDebugPrimitiveTile> Tiles = new List<HeightfieldDebugPrimitiveTile>();

            public IEnumerable<MeshDraw> GetMeshDraws()
            {
                return Tiles.Select((t) => t.MeshDraw);
            }
        }
    }
}
