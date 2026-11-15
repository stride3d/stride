// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Physics;

public unsafe class HeightfieldColliderShape : ColliderShape
{
    public static readonly int MinimumHeightStickWidth = 2;
    public static readonly int MinimumHeightStickLength = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeightfieldColliderShape"/> class with <see cref="short"/> height data.
    /// </summary>
    /// <param name="heightStickWidth">The width of the height field.</param>
    /// <param name="heightStickLength">The length of the height field.</param>
    /// <param name="heightData">The height data. This will be copied.</param>
    /// <param name="heightScale">The scale of the height values.</param>
    /// <param name="minHeight">The minimum height value.</param>
    /// <param name="maxHeight">The maximum height value.</param>
    /// <param name="flipQuadEdges">Whether to flip the quad edges.</param>
    public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, ReadOnlySpan<short> heightData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        : this(heightStickWidth, heightStickLength, HeightfieldTypes.Short, MemoryMarshal.AsBytes(heightData), true, heightScale, minHeight, maxHeight, flipQuadEdges)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeightfieldColliderShape"/> class with <see cref="byte"/> height data.
    /// </summary>
    /// <param name="heightStickWidth">The width of the height field.</param>
    /// <param name="heightStickLength">The length of the height field.</param>
    /// <param name="heightData">The height data. This will be copied.</param>
    /// <param name="heightScale">The scale of the height values.</param>
    /// <param name="minHeight">The minimum height value.</param>
    /// <param name="maxHeight">The maximum height value.</param>
    /// <param name="flipQuadEdges">Whether to flip the quad edges.</param>
    public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, ReadOnlySpan<byte> heightData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        : this(heightStickWidth, heightStickLength, HeightfieldTypes.Byte, heightData, true, heightScale, minHeight, maxHeight, flipQuadEdges)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeightfieldColliderShape"/> class with <see cref="float"/> height data.
    /// </summary>
    /// <param name="heightStickWidth">The width of the height field.</param>
    /// <param name="heightStickLength">The length of the height field.</param>
    /// <param name="heightData">The height data. This will be copied.</param>
    /// <param name="heightScale">The scale of the height values.</param>
    /// <param name="minHeight">The minimum height value.</param>
    /// <param name="maxHeight">The maximum height value.</param>
    /// <param name="flipQuadEdges">Whether to flip the quad edges.</param>
    public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, ReadOnlySpan<float> heightData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        : this(heightStickWidth, heightStickLength, HeightfieldTypes.Float, MemoryMarshal.AsBytes(heightData), true, heightScale, minHeight, maxHeight, flipQuadEdges)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeightfieldColliderShape"/> class with height data initialized to zero.
    /// </summary>
    /// <remarks>
    /// The height data can be set later using the <see cref="ShortSpan"/>, <see cref="ByteSpan"/>, or <see cref="FloatSpan"/> properties,
    /// depending on the <paramref name="heightType"/> specified. Make sure to use the appropriate locking mechanism (<see cref="LockToReadAndWriteHeights"/>)
    /// when writing with these spans to ensure thread safety.
    /// </remarks>
    /// <param name="heightStickWidth">The width of the height field.</param>
    /// <param name="heightStickLength">The length of the height field.</param>
    /// <param name="heightType">The type of the height data.</param>
    /// <param name="heightScale">The scale of the height values.</param>
    /// <param name="minHeight">The minimum height value.</param>
    /// <param name="maxHeight">The maximum height value.</param>
    /// <param name="flipQuadEdges">Whether to flip the quad edges.</param>
    public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, HeightfieldTypes heightType, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        : this(heightStickWidth, heightStickLength, heightType, [], false, heightScale, minHeight, maxHeight, flipQuadEdges)
    {
    }

    private HeightfieldColliderShape
    (
        int heightStickWidth,
        int heightStickLength,
        HeightfieldTypes heightType,
        ReadOnlySpan<byte> heightDataBytes,
        bool hasHeightData,
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

        var scalarType = heightType switch
        {
            HeightfieldTypes.Short => BulletSharp.PhyScalarType.Int16,
            HeightfieldTypes.Byte => BulletSharp.PhyScalarType.Byte,
            HeightfieldTypes.Float => BulletSharp.PhyScalarType.Single,
            _ => throw new ArgumentOutOfRangeException(nameof(heightType), heightType, null),
        };

        var scalarSize = heightType switch
        {
            HeightfieldTypes.Short => sizeof(short),
            HeightfieldTypes.Byte => sizeof(byte),
            HeightfieldTypes.Float => sizeof(float),
            _ => throw new ArgumentOutOfRangeException(nameof(heightType), heightType, null),
        };

        var expectedDataLength = HeightStickWidth * HeightStickLength * scalarSize;
        if (hasHeightData)
        {
            if (heightDataBytes.Length != expectedDataLength)
            {
                throw new ArgumentException($"The length of {nameof(heightDataBytes)} is invalid. Expected: {expectedDataLength}, Actual: {heightDataBytes.Length}");
            }

            heightDataPointer = NativeMemory.Alloc((nuint)heightDataBytes.Length);
            heightDataBytes.CopyTo(new Span<byte>(heightDataPointer, heightDataBytes.Length));
        }
        else
        {
            heightDataPointer = NativeMemory.AllocZeroed((nuint)expectedDataLength);
        }

        InternalShape = new BulletSharp.HeightfieldTerrainShape(HeightStickWidth, HeightStickLength, (nint)heightDataPointer, HeightScale, MinHeight, MaxHeight, 1, scalarType, flipQuadEdges)
        {
            LocalScaling = cachedScaling,
        };

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

    /// <summary>
    /// A live view of the native height data as a span of shorts.
    /// </summary>
    /// <remarks>
    /// This is only valid if the height type is <see cref="HeightfieldTypes.Short"/>.
    /// This should only be used under a <see cref="LockToReadHeights"/> or <see cref="LockToReadAndWriteHeights"/> context to ensure thread safety.
    /// This must never be used after the shape has been disposed.
    /// </remarks>
    public Span<short> ShortSpan => HeightType == HeightfieldTypes.Short && heightDataPointer != null ? new Span<short>(heightDataPointer, HeightStickWidth * HeightStickLength) : [];

    /// <summary>
    /// A live view of the native height data as a span of bytes.
    /// </summary>
    /// <remarks>
    /// This is only valid if the height type is <see cref="HeightfieldTypes.Byte"/>.
    /// This should only be used under a <see cref="LockToReadHeights"/> or <see cref="LockToReadAndWriteHeights"/> context to ensure thread safety.
    /// This must never be used after the shape has been disposed.
    /// </remarks>
    public Span<byte> ByteSpan => HeightType == HeightfieldTypes.Byte && heightDataPointer != null ? new Span<byte>(heightDataPointer, HeightStickWidth * HeightStickLength) : [];

    /// <summary>
    /// A live view of the native height data as a span of floats.
    /// </summary>
    /// <remarks>
    /// This is only valid if the height type is <see cref="HeightfieldTypes.Float"/>.
    /// This should only be used under a <see cref="LockToReadHeights"/> or <see cref="LockToReadAndWriteHeights"/> context to ensure thread safety.
    /// This must never be used after the shape has been disposed.
    /// </remarks>
    public Span<float> FloatSpan => HeightType == HeightfieldTypes.Float && heightDataPointer != null ? new Span<float>(heightDataPointer, HeightStickWidth * HeightStickLength) : [];

    /// <summary>
    /// Whether this shape has height data allocated. If false, this shape might have been disposed.
    /// </summary>
    public bool HasHeightData => heightDataPointer != null;

    private void* heightDataPointer;

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

        heightfieldDebugPrimitive?.Update(commandList);
    }

    private readonly ReaderWriterLockSlim lockSlim = new();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (heightDataPointer != null)
        {
            using (LockToReadAndWriteHeights())
            {
                NativeMemory.Free(heightDataPointer);
                heightDataPointer = null;
            }
        }

        if (disposing)
        {
            lockSlim.Dispose();
        }
    }

    public HeightArrayLock LockToReadHeights()
    {
        return new HeightArrayLock(HeightArrayLock.LockTypes.Read, lockSlim);
    }

    public HeightArrayLock LockToReadAndWriteHeights()
    {
        return new HeightArrayLock(HeightArrayLock.LockTypes.ReadWrite, lockSlim);
    }

    public sealed class HeightArrayLock : IDisposable
    {
        public enum LockTypes
        {
            Read = 0,
            ReadWrite = 1,
        }

        private readonly ReaderWriterLockSlim readerWriterLockSlim;

        internal HeightArrayLock(LockTypes lockType, ReaderWriterLockSlim lockSlim)
        {
            ArgumentNullException.ThrowIfNull(lockSlim);

            readerWriterLockSlim = lockSlim;

            switch (lockType)
            {
                case LockTypes.Read:
                    readerWriterLockSlim.EnterReadLock();
                    break;
                case LockTypes.ReadWrite:
                    readerWriterLockSlim.EnterWriteLock();
                    break;
                default:
                    throw new ArgumentException($"{ nameof(lockType) } is invalid type");
            }
        }

        public void Unlock()
        {
            if (readerWriterLockSlim.IsReadLockHeld)
            {
                readerWriterLockSlim.ExitReadLock();
            }
            else if (readerWriterLockSlim.IsWriteLockHeld)
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            Unlock();
        }
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

        public readonly List<Tile> Tiles = [];

        private HeightfieldColliderShape heightfield;

        private HeightfieldDebugPrimitive(HeightfieldColliderShape heightfieldColliderShape)
        {
            heightfield = heightfieldColliderShape;
        }

        private void GetHeightStickHeightAndColor(int x, int y, out float heightStickHeight, out Color heightStickColor)
        {
            var index = y * heightfield.HeightStickWidth + x;

            heightStickHeight = heightfield.HeightType switch
            {
                HeightfieldTypes.Short => heightfield.ShortSpan[index] * heightfield.HeightScale,
                HeightfieldTypes.Byte => heightfield.ByteSpan[index] * heightfield.HeightScale,
                HeightfieldTypes.Float => heightfield.FloatSpan[index],
                _ => throw new NotSupportedException()
            };

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
            var vertexBuffer = Buffer.Vertex.New(device, vertices, GraphicsResourceUsage.Dynamic);
            var indexBuffer = Buffer.Index.New(device, indices);

            var meshDraw = new MeshDraw
            {
                PrimitiveType = PrimitiveType.TriangleList,
                VertexBuffers = [new VertexBufferBinding(vertexBuffer, VertexPositionNormalColor.Layout, vertexBuffer.ElementCount)],
                IndexBuffer = new IndexBufferBinding(indexBuffer, false, indexBuffer.ElementCount),
                StartLocation = 0,
                DrawCount = indexBuffer.ElementCount,
            };

            return meshDraw;
        }

        public void Update(CommandList commandList)
        {
            Dispatcher.ForEach(Tiles, this, UpdateTile);

            static void UpdateTile(HeightfieldDebugPrimitive shape, Tile tile)
            {
                for (int j = 0; j <= tile.Height; ++j)
                {
                    for (int i = 0; i <= tile.Width; ++i)
                    {
                        shape.GetHeightStickHeightAndColor(tile.Point.X + i, tile.Point.Y + j, out var heightStickHeight, out var color);

                        var index = j * (tile.Width + 1) + i;
                        tile.Vertices[index].Position.Y = heightStickHeight;
                        tile.Vertices[index].Color = color;
                    }
                }
            }

            foreach (var tile in Tiles)
            {
                tile.MeshDraw.VertexBuffers[0].Buffer.SetData(commandList, tile.Vertices);
            }
        }

        public IEnumerable<MeshDraw> GetMeshDraws()
        {
            return Tiles.Select(t => t.MeshDraw);
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
