// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="AssetNodeContainer.IsPrimitiveType"/>. This filter is what decides whether a
/// CLR type is treated as an opaque leaf value in the Quantum graph (no sub-nodes) or expanded into a graph
/// of its own. Engine value types such as <see cref="Vector3"/> or <see cref="AssetId"/> must stay leaves,
/// otherwise the asset graph would explode into their internal fields and overrides would be tracked at the
/// wrong granularity.
/// </summary>
public class TestAssetNodeContainer
{
    private static bool IsPrimitive(Type type) => new AssetNodeContainer().IsPrimitiveType(type);

    [Theory]
    // Real CLR primitives and enums.
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(double))]
    [InlineData(typeof(long))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(char))]
    [InlineData(typeof(DayOfWeek))]
    // Sealed BCL/engine value types that are explicitly whitelisted as leaves.
    [InlineData(typeof(string))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(AssetId))]
    [InlineData(typeof(Color))]
    [InlineData(typeof(Color3))]
    [InlineData(typeof(Color4))]
    [InlineData(typeof(Vector2))]
    [InlineData(typeof(Vector3))]
    [InlineData(typeof(Vector4))]
    [InlineData(typeof(Int2))]
    [InlineData(typeof(Int3))]
    [InlineData(typeof(Int4))]
    [InlineData(typeof(Quaternion))]
    [InlineData(typeof(RectangleF))]
    [InlineData(typeof(Rectangle))]
    [InlineData(typeof(Matrix))]
    [InlineData(typeof(AngleSingle))]
    // Types matched by assignability to an abstract/base primitive type.
    [InlineData(typeof(UPath))]
    [InlineData(typeof(UFile))]
    [InlineData(typeof(UDirectory))]
    [InlineData(typeof(AssetReference))] // implements IReference
    [InlineData(typeof(PropertyKey))]
    public void TestEngineLeafTypesAreTreatedAsPrimitive(Type type)
    {
        Assert.True(IsPrimitive(type));
    }

    [Theory]
    [InlineData(typeof(Types.SomeObject))]
    [InlineData(typeof(Types.MyAsset1))]
    [InlineData(typeof(List<string>))]
    [InlineData(typeof(int[]))]
    [InlineData(typeof(object))]
    public void TestCompoundTypesAreNotPrimitive(Type type)
    {
        Assert.False(IsPrimitive(type));
    }

    [Fact]
    public void TestNullableLeafTypesAreUnwrapped()
    {
        // Nullable<T> must be classified as its underlying T, otherwise nullable value-type members would expand.
        Assert.True(IsPrimitive(typeof(int?)));
        Assert.True(IsPrimitive(typeof(Vector3?)));
        Assert.True(IsPrimitive(typeof(AssetId?)));
    }
}
