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

                    InternalShape = new BulletSharp.HeightfieldTerrainShape(HeightStickWidth, HeightStickLength, ShortArray.Pointer, HeightScale, MinHeight, MaxHeight, 1, BulletSharp.PhyScalarType.Int16, flipQuadEdges)
                    {
                        LocalScaling = cachedScaling,
                    };

                    break;

                case HeightfieldTypes.Byte:
                    ByteArray = dynamicFieldData as UnmanagedArray<byte>;

                    InternalShape = new BulletSharp.HeightfieldTerrainShape(HeightStickWidth, HeightStickLength, ByteArray.Pointer, HeightScale, MinHeight, MaxHeight, 1, BulletSharp.PhyScalarType.Byte, flipQuadEdges)
                    {
                        LocalScaling = cachedScaling,
                    };

                    break;

                case HeightfieldTypes.Float:
                    FloatArray = dynamicFieldData as UnmanagedArray<float>;

                    InternalShape = new BulletSharp.HeightfieldTerrainShape(HeightStickWidth, HeightStickLength, FloatArray.Pointer, HeightScale, MinHeight, MaxHeight, 1, BulletSharp.PhyScalarType.Single, flipQuadEdges)
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
            set { ((BulletSharp.HeightfieldTerrainShape)InternalShape).SetUseDiamondSubdivision(value); }
        }

        public bool UseZigzagSubdivision
        {
            set { ((BulletSharp.HeightfieldTerrainShape)InternalShape).SetUseZigzagSubdivision(value); }
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

        public override IDebugPrimitive CreateUpdatableDebugPrimitive(GraphicsDevice graphicsDevice)
        {
            return HeightfieldDebugPrimitive.New(graphicsDevice, this);
        }

        public override void UpdateDebugPrimitive(CommandList commandList, IDebugPrimitive debugPrimitive)
        {
            var heightfieldDebugPrimitive = debugPrimitive as HeightfieldDebugPrimitive;

            if (heightfieldDebugPrimitive == null)
            {
                return;
            }

            heightfieldDebugPrimitive.Update(commandList);
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

        public class HeightfieldDebugPrimitive : IDebugPrimitive
        {
            private static readonly int MaxTileWidth = 64;
            private static readonly int MaxTileHeight = 64;

            public class Tile
            {
                public Point Point;
                public int Width;
                public int Height;
                public VertexPositionNormalColor[] Vertices;
                public MeshDraw MeshDraw;
            }

            public readonly List<Tile> Tiles = new List<Tile>();

            private HeightfieldColliderShape heightfield;

            private HeightfieldDebugPrimitive(HeightfieldColliderShape heightfieldColliderShape)
            {
                heightfield = heightfieldColliderShape;
            }

            private void GetHeightStickHeightAndColor(int x, int y, out float heightStickHeight, out Color heightStickColor)
            {
                var index = y * heightfield.HeightStickWidth + x;

                switch (heightfield.HeightType)
                {
                    case HeightfieldTypes.Short:
                        heightStickHeight = heightfield.ShortArray[index] * heightfield.HeightScale;
                        break;

                    case HeightfieldTypes.Byte:
                        heightStickHeight = heightfield.ByteArray[index] * heightfield.HeightScale;
                        break;

                    case HeightfieldTypes.Float:
                        heightStickHeight = heightfield.FloatArray[index];
                        break;

                    default:
                        throw new NotSupportedException();
                }

                if (heightfield.MinHeight <= heightStickHeight && heightStickHeight <= heightfield.MaxHeight)
                {
                    heightStickColor = Color.White;
                }
                else
                {
                    heightStickColor = Color.Black;
                }
            }

            private void CreateTileMeshData(Point point, int width, int height, Vector3 offset, out VertexPositionNormalColor[] vertices, out ushort[] indices)
            {
                vertices = new VertexPositionNormalColor[(width + 1) * (height + 1)];

                ushort GetIndex(int x, int y) => (ushort)(y * (width + 1) + x);

                for (int j = 0; j <= height; ++j)
                {
                    for (int i = 0; i <= width; ++i)
                    {
                        GetHeightStickHeightAndColor(point.X + i, point.Y + j, out var heightStickHeight, out var color);
                        vertices[GetIndex(i, j)] = new VertexPositionNormalColor(
                            offset + new Vector3(i, heightStickHeight, j),
                            Vector3.UnitY,
                            color);
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

            private MeshDraw CreateTileMeshDraw(GraphicsDevice device, VertexPositionNormalColor[] vertices, ushort[] indices)
            {
                var vertexBuffer = Buffer.Vertex.New(device, vertices, GraphicsResourceUsage.Dynamic).RecreateWith(vertices);
                var indexBuffer = Buffer.Index.New(device, indices).RecreateWith(indices);

                var meshDraw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    VertexBuffers = new VertexBufferBinding[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalColor.Layout, vertexBuffer.ElementCount) },
                    IndexBuffer = new IndexBufferBinding(indexBuffer, false, indexBuffer.ElementCount),
                    StartLocation = 0,
                    DrawCount = indexBuffer.ElementCount,
                };

                return meshDraw;
            }

            public void Update(CommandList commandList)
            {
                Dispatcher.ForEach(Tiles, (tile) =>
                {
                    for (int j = 0; j <= tile.Height; ++j)
                    {
                        for (int i = 0; i <= tile.Width; ++i)
                        {
                            GetHeightStickHeightAndColor(tile.Point.X + i, tile.Point.Y + j, out var heightStickHeight, out var color);

                            var index = j * (tile.Width + 1) + i;
                            tile.Vertices[index].Position.Y = heightStickHeight;
                            tile.Vertices[index].Color = color;
                        }
                    }
                });

                foreach (var tile in Tiles)
                {
                    tile.MeshDraw.VertexBuffers[0].Buffer.SetData(commandList, tile.Vertices);
                }
            }

            public IEnumerable<MeshDraw> GetMeshDraws()
            {
                return Tiles.Select((t) => t.MeshDraw);
            }

            public static HeightfieldDebugPrimitive New(GraphicsDevice device, HeightfieldColliderShape heightfieldColliderShape)
            {
                var debugPrimitive = new HeightfieldDebugPrimitive(heightfieldColliderShape);

                var width = heightfieldColliderShape.HeightStickWidth - 1;
                var height = heightfieldColliderShape.HeightStickLength - 1;

                var offset = new Vector3(-(width * 0.5f), 0, -(height * 0.5f));

                for (int j = 0; j < height; j += MaxTileHeight)
                {
                    for (int i = 0; i < width; i += MaxTileWidth)
                    {
                        var tileWidth = Math.Min(MaxTileWidth, width - i);
                        var tileHeight = Math.Min(MaxTileHeight, height - j);

                        var point = new Point(i, j);

                        debugPrimitive.CreateTileMeshData(point, tileWidth, tileHeight, offset + new Vector3(i, 0, j), out var vertices, out var indices);

                        var tile = new Tile
                        {
                            Point = point,
                            Width = tileWidth,
                            Height = tileHeight,
                            Vertices = vertices,
                            MeshDraw = debugPrimitive.CreateTileMeshDraw(device, vertices, indices),
                        };

                        debugPrimitive.Tiles.Add(tile);
                    }
                }

                return debugPrimitive;
            }
        }
    }
}
