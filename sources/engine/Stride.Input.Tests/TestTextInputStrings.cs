// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
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

    [Fact]
    public void DecodingSingleAsciiCharacterDoesNotAllocate()
    {
        var buffer = (byte*)NativeMemory.AllocZeroed(SDLTextBufferSize);
        try
        {
            buffer[0] = (byte)'a';

            var measurement = GCMeasure.Run(() => TextInputStrings.FromNullTerminatedUtf8(buffer, SDLTextBufferSize));

            Assert.True(measurement.AllocatedBytes == 0, $"Decoding \"a\" allocated. Measured {measurement}.");
        }
        finally
        {
            NativeMemory.Free(buffer);
        }
    }

    [Fact]
    public void DecodingNonAsciiAllocatesOnlyTheResultString()
    {
        const string text = "日本";
        var buffer = (byte*)NativeMemory.AllocZeroed(SDLTextBufferSize);
        try
        {
            Encoding.UTF8.GetBytes(text, new Span<byte>(buffer, SDLTextBufferSize));

            var decoding = GCMeasure.Run(() => TextInputStrings.FromNullTerminatedUtf8(buffer, SDLTextBufferSize));
            var oneString = GCMeasure.Run(() => new string('x', text.Length));

            // The runtime can add a little to either measurement (Mono on iOS does), so a scratch buffer shows up as
            // decoding costing more than creating the string on its own, not as the two differing at all
            Assert.True(
                decoding.AllocatedBytes <= oneString.AllocatedBytes,
                $"Decoding \"{text}\" should allocate only the result string ({oneString}). Measured {decoding}.");
        }
        finally
        {
            NativeMemory.Free(buffer);
        }
    }

    [Theory]
    [InlineData("a")]
    [InlineData("é")]
    [InlineData("日本")]
    [InlineData("")]
    public void DecodesUtf16(string expected)
    {
        Assert.Equal(expected, TextInputStrings.FromUtf16(expected.ToCharArray()));
    }

    [Fact]
    public void DecodingSingleAsciiUtf16CharacterDoesNotAllocate()
    {
        var text = new[] { 'a' };

        var measurement = GCMeasure.Run(() => TextInputStrings.FromUtf16(text));

        Assert.True(measurement.AllocatedBytes == 0, $"Decoding \"a\" allocated. Measured {measurement}.");
    }

    private static byte[] NullTerminatedBuffer(string text)
    {
        var buffer = new byte[SDLTextBufferSize];
        Encoding.UTF8.GetBytes(text, buffer);
        return buffer;
    }
}
