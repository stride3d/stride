// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests;

/// <summary>
///   Calling <c>Buffer.Raw.New</c> or <c>Buffer.Argument.New</c> with an <c>int</c> size creates a buffer of that size,
///   not a 4-byte buffer holding the <c>int</c>.
/// </summary>
public class TestBufferSizeOverloads : GraphicTestGameBase
{
    private const int Size = 256;

    private void Check(Func<GraphicsDevice, Buffer> create)
    {
        PerformTest(game =>
        {
            using var buffer = create(game.GraphicsDevice);
            Assert.Equal(Size, buffer.SizeInBytes);
        });
    }

    [SkippableFact]
    public void RawSizeFlags() => Check(d => { int size = Size; return Buffer.Raw.New(d, size, BufferFlags.UnorderedAccess); });

    [SkippableFact]
    public void RawSizeFlagsUsage() => Check(d => { int size = Size; return Buffer.Raw.New(d, size, BufferFlags.UnorderedAccess, GraphicsResourceUsage.Default); });

    [SkippableFact]
    public void ArgumentSize() => Check(d => { int size = Size; return Buffer.Argument.New(d, size); });

    [SkippableFact]
    public void ArgumentSizeUsage() => Check(d => { int size = Size; return Buffer.Argument.New(d, size, GraphicsResourceUsage.Default); });
}
