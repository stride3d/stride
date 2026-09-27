// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers decoding the text carried by platform text input events.
/// </summary>
public unsafe class TestTextInputStrings
{
    private const int SDLTextBufferSize = 32;

    [Theory]
    [InlineData("a")]
    [InlineData("é")]
    [InlineData("日本")]
    [InlineData("")]
    public void DecodesNullTerminatedUtf8(string expected)
    {
        var buffer = NullTerminatedBuffer(expected);

        fixed (byte* text = buffer)
        {
            Assert.Equal(expected, TextInputStrings.FromNullTerminatedUtf8(text, SDLTextBufferSize));
        }
    }

    [Fact]
    public void StopsAtCapacityWhenNoTerminatorIsFound()
    {
        var buffer = Encoding.UTF8.GetBytes("abcdef");

        fixed (byte* text = buffer)
        {
            Assert.Equal("abc", TextInputStrings.FromNullTerminatedUtf8(text, 3));
        }
    }

    private static byte[] NullTerminatedBuffer(string text)
    {
        var buffer = new byte[SDLTextBufferSize];
        Encoding.UTF8.GetBytes(text, buffer);
        return buffer;
    }
}
