// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;
using Stride.Rendering;

namespace Stride.Shaders.Parsers.Tests;

public class ParameterKeyHashTests
{
    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("12345678")]
    [InlineData("a longer parameter name")]
    [InlineData("光.😀")]
    public unsafe void HashMatchesLegacyByteRepresentation(string name)
    {
        var builder = new ObjectIdBuilder();
        fixed (char* chars = name)
        {
#pragma warning disable CS0618 // Verify compatibility with hashes produced by the legacy overload.
            builder.Write((byte*)chars, sizeof(char) * name.Length);
#pragma warning restore CS0618
        }

        var id = builder.ComputeHash();
        var words = (ulong*)&id;
        var expected = words[0] ^ words[1];

        Assert.Equal(expected, ParameterKeys.NewValue<int>(name: name).HashCode);
    }
}
