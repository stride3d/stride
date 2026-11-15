// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text;
using Stride.Core.UnsafeExtensions;
using Xunit;

namespace Stride.Core.Tests;

public class TestStringMarshal
{
    [Fact]
    public void DefaultSpanGivesNull()
    {
        Assert.Null(default(ReadOnlySpan<byte>).GetString());
        Assert.Null(default(ReadOnlySpan<ushort>).GetString());
        Assert.Null(default(ReadOnlySpan<char>).GetString());
    }

    [Fact]
    public void SpanOverAnEmptyArrayGivesAnEmptyString()
    {
        Assert.Equal(string.Empty, new ReadOnlySpan<byte>([]).GetString());
        Assert.Equal(string.Empty, new ReadOnlySpan<ushort>([]).GetString());
        Assert.Equal(string.Empty, new ReadOnlySpan<char>([]).GetString());
    }

    [Fact]
    public void SpanWithContentGivesTheString()
    {
        const string text = "Stride é";

        Assert.Equal(text, new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(text)).GetString());
        Assert.Equal(text, new ReadOnlySpan<ushort>(Array.ConvertAll(text.ToCharArray(), c => (ushort) c)).GetString());
        Assert.Equal(text, text.AsSpan().GetString());
    }
}
