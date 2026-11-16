// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    /// <summary>
    ///   Calling a <c>Buffer.Vertex.New</c>, <c>Buffer.Index.New</c>, <c>Buffer.Constant.New</c>, <c>Buffer.Raw.New</c> or <c>Buffer.Argument.New</c> helper with an <c>int</c> size creates a buffer of that size,
    ///   not a 4-byte buffer holding the <c>int</c>.
    /// </summary>
    public class TestBufferSizeOverloads : GraphicTestGameBase
    {
        private const int Size = 256;

        private void Check(System.Func<GraphicsDevice, Buffer> create, int expectedSize = Size)
        {
            PerformTest(
                game =>
                {
                    using var buffer = create(game.GraphicsDevice);
                    Assert.Equal(expectedSize, buffer.SizeInBytes);
                });
        }

        [SkippableFact]
        public void VertexSize() => Check(d => { int size = Size; return Buffer.Vertex.New(d, size); });

        [SkippableFact]
        public void VertexSizeUsage() => Check(d => { int size = Size; return Buffer.Vertex.New(d, size, GraphicsResourceUsage.Dynamic); });

        [SkippableFact]
        public void VertexExplicitTypeArgumentCountsElements() => Check(d => { int count = 10; return Buffer.Vertex.New<int>(d, count, GraphicsResourceUsage.Dynamic); }, 10 * sizeof(int));

        [SkippableFact]
        public void VertexInValueMakesBufferOfValue() => Check(d => { int value = Size; return Buffer.Vertex.New(d, in value, GraphicsResourceUsage.Immutable); }, sizeof(int));

        [SkippableFact]
        public void IndexSize() => Check(d => { int size = Size; return Buffer.Index.New(d, size); });

        [SkippableFact]
        public void IndexSizeUsage() => Check(d => { int size = Size; return Buffer.Index.New(d, size, GraphicsResourceUsage.Dynamic); });

        [SkippableFact]
        public void ConstantSize() => Check(d => { int size = Size; return Buffer.Constant.New(d, size); });

        [SkippableFact]
        public void ConstantSizeUsage() => Check(d => { int size = Size; return Buffer.Constant.New(d, size, GraphicsResourceUsage.Dynamic); });

        [SkippableFact]
        public void RawSizeFlags() => Check(d => { int size = Size; return Buffer.Raw.New(d, size, BufferFlags.UnorderedAccess); });

        [SkippableFact]
        public void RawSizeFlagsUsage() => Check(d => { int size = Size; return Buffer.Raw.New(d, size, BufferFlags.UnorderedAccess, GraphicsResourceUsage.Default); });

        [SkippableFact]
        public void ArgumentSize() => Check(d => { int size = Size; return Buffer.Argument.New(d, size); });

        [SkippableFact]
        public void ArgumentSizeUsage() => Check(d => { int size = Size; return Buffer.Argument.New(d, size, GraphicsResourceUsage.Default); });
    }
}
