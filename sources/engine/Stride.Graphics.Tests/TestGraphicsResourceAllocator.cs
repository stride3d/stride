// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests;

public class TestGraphicsResourceAllocator : GameTestBase
{
    private static TextureDescription Description => TextureDescription.New2D(16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);

    [Fact]
    public void ReusesTextureWhenCreatedDescriptionDiffersFromRequest()
    {
        PerformTest(game =>
        {
            using var allocator = new SubstitutingAllocator(game.GraphicsDevice);
            var first = allocator.GetTemporaryTexture(Description);
            Assert.NotEqual(Description, first.Description);
            allocator.ReleaseReference(first);

            // Simulate repeated frames. This must not depend on a particular GPU's D24 support.
            for (var frame = 0; frame < 100; frame++)
            {
                var texture = allocator.GetTemporaryTexture(Description);
                Assert.Same(first, texture);
                allocator.ReleaseReference(texture);
            }

            Assert.Equal(1, allocator.CreationCount);
        });
    }

    [Fact]
    public void KeepsTextureUnavailableUntilFinalReferenceIsReleased()
    {
        PerformTest(game =>
        {
            using var allocator = new SubstitutingAllocator(game.GraphicsDevice);
            var first = allocator.GetTemporaryTexture(Description);
            allocator.AddReference(first);
            allocator.ReleaseReference(first);

            var second = allocator.GetTemporaryTexture(Description);
            Assert.NotSame(first, second);
            allocator.Recycle();
            Assert.False(first.IsDisposed);
            Assert.False(second.IsDisposed);

            allocator.ReleaseReference(first);
            var reused = allocator.GetTemporaryTexture(Description);
            Assert.Same(first, reused);
            allocator.ReleaseReference(reused);
            allocator.ReleaseReference(second);
        });
    }

    [Fact]
    public void RecyclingDisposesUnusedTextureAndForgetsItsOwnership()
    {
        PerformTest(game =>
        {
            using var allocator = new SubstitutingAllocator(game.GraphicsDevice);
            var first = allocator.GetTemporaryTexture(Description);
            allocator.ReleaseReference(first);

            allocator.Recycle(_ => false);
            Assert.False(first.IsDisposed);
            var reused = allocator.GetTemporaryTexture(Description);
            Assert.Same(first, reused);
            allocator.ReleaseReference(reused);

            allocator.Recycle(_ => true);
            Assert.True(first.IsDisposed);
            Assert.Throws<ArgumentException>(() => allocator.AddReference(first));
            Assert.Throws<ArgumentException>(() => allocator.ReleaseReference(first));

            var replacement = allocator.GetTemporaryTexture(Description);
            Assert.NotSame(first, replacement);
            Assert.False(replacement.IsDisposed);
            allocator.ReleaseReference(replacement);
        });
    }

    [Fact]
    public void RejectsForeignResourcesAndReferenceUnderflow()
    {
        PerformTest(game =>
        {
            using var allocator = new SubstitutingAllocator(game.GraphicsDevice);
            using var otherAllocator = new SubstitutingAllocator(game.GraphicsDevice);
            var texture = allocator.GetTemporaryTexture(Description);
            Assert.Throws<ArgumentException>(() => otherAllocator.AddReference(texture));
            Assert.Throws<ArgumentException>(() => otherAllocator.ReleaseReference(texture));

            allocator.ReleaseReference(texture);
            Assert.Throws<ArgumentException>(() => allocator.ReleaseReference(texture));
            Assert.Same(texture, allocator.GetTemporaryTexture(Description));
            allocator.ReleaseReference(texture);
        });
    }

    [Fact]
    public void ReusesBuffersAfterFinalRelease()
    {
        PerformTest(game =>
        {
            using var allocator = new GraphicsResourceAllocator(game.GraphicsDevice);
            var description = new BufferDescription(64, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
            var buffer = allocator.GetTemporaryBuffer(description);
            allocator.AddReference(buffer);
            allocator.ReleaseReference(buffer);
            var otherBuffer = allocator.GetTemporaryBuffer(description);
            Assert.NotSame(buffer, otherBuffer);
            allocator.ReleaseReference(buffer);
            Assert.Same(buffer, allocator.GetTemporaryBuffer(description));
            allocator.ReleaseReference(buffer);
            allocator.ReleaseReference(otherBuffer);
        });
    }

    [SkippableFact]
    public void ReusesQueryPoolsImmediately()
    {
        PerformTest(game =>
        {
            // Vulkan keeps a released query pool referenced until the GPU is done with it, so it is not reused immediately
            Skip.If(GraphicsDevice.Platform == GraphicsPlatform.Vulkan,
                "Vulkan reuses a released query pool only after the GPU is done with it.");

            using var allocator = new GraphicsResourceAllocator(game.GraphicsDevice);
            var pool = allocator.GetQueryPool(QueryType.Timestamp, 2);
            var otherPool = allocator.GetQueryPool(QueryType.Timestamp, 2);
            Assert.NotSame(pool, otherPool);
            allocator.ReleaseReference(pool);
            Assert.Same(pool, allocator.GetQueryPool(QueryType.Timestamp, 2));
            allocator.ReleaseReference(pool);
            allocator.ReleaseReference(otherPool);
        });
    }

    [Fact]
    public void DestroyDisposesResourcesInAllCaches()
    {
        PerformTest(game =>
        {
            var allocator = new SubstitutingAllocator(game.GraphicsDevice);
            Texture texture;
            Buffer buffer;
            QueryPool pool;
            using (allocator)
            {
                texture = allocator.GetTemporaryTexture(Description);
                buffer = allocator.GetTemporaryBuffer(new BufferDescription(64, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default));
                pool = allocator.GetQueryPool(QueryType.Timestamp, 2);
                allocator.ReleaseReference(texture);
                // Destruction must also dispose resources still checked out by callers.
            }

            Assert.True(texture.IsDisposed);
            Assert.True(buffer.IsDisposed);
            Assert.True(pool.IsDisposed);
            Assert.Throws<ArgumentException>(() => allocator.ReleaseReference(texture));
            Assert.Throws<ArgumentException>(() => allocator.ReleaseReference(buffer));
            Assert.Throws<ArgumentException>(() => allocator.ReleaseReference(pool));
        });
    }

    private sealed class SubstitutingAllocator(GraphicsDevice device) : GraphicsResourceAllocator(device)
    {
        public int CreationCount { get; private set; }

        protected override Texture CreateTexture(TextureDescription description, PixelFormat viewFormat)
        {
            CreationCount++;
            // Exercise backend description substitution using an ordinary color texture on every API.
            description.Width *= 2;
            return base.CreateTexture(description, viewFormat);
        }
    }
}
