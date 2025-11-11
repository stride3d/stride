// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Stride.Core.Mathematics;
using Stride.Core.TypeConverters;
using Xunit;

namespace Stride.Core.Design.Tests.TypeConverters;

public class TestMatrixConverter
{
    private readonly MatrixConverter converter = new();

    [Fact]
    public void Constructor_InitializesPropertiesWithAllMatrixFields()
    {
        var properties = converter.GetProperties(null, Matrix.Identity, null);

        Assert.NotNull(properties);
        Assert.Equal(16, properties.Count);
        Assert.NotNull(properties["M11"]);
        Assert.NotNull(properties["M12"]);
        Assert.NotNull(properties["M13"]);
        Assert.NotNull(properties["M14"]);
        Assert.NotNull(properties["M21"]);
        Assert.NotNull(properties["M22"]);
        Assert.NotNull(properties["M23"]);
        Assert.NotNull(properties["M24"]);
        Assert.NotNull(properties["M31"]);
        Assert.NotNull(properties["M32"]);
        Assert.NotNull(properties["M33"]);
        Assert.NotNull(properties["M34"]);
        Assert.NotNull(properties["M41"]);
        Assert.NotNull(properties["M42"]);
        Assert.NotNull(properties["M43"]);
        Assert.NotNull(properties["M44"]);
    }

    [Fact]
    public void ConvertTo_ToStringWithMatrix_ReturnsMatrixString()
    {
        var matrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, matrix, typeof(string));

        Assert.NotNull(result);
        Assert.IsType<string>(result);
        var str = (string)result;
        Assert.Contains("1", str);
        Assert.Contains("16", str);
    }

    [Fact]
    public void ConvertTo_ToInstanceDescriptor_ReturnsInstanceDescriptor()
    {
        var matrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, matrix, typeof(InstanceDescriptor));

        Assert.NotNull(result);
        var descriptor = Assert.IsType<InstanceDescriptor>(result);
        Assert.NotNull(descriptor.MemberInfo);
    }

    [Fact]
    public void ConvertTo_WithNullDestinationType_ThrowsArgumentNullException()
    {
        var matrix = Matrix.Identity;

        Assert.Throws<ArgumentNullException>(() =>
            converter.ConvertTo(null, CultureInfo.InvariantCulture, matrix, null!));
    }

    [Fact]
    public void ConvertTo_WithNonMatrixValue_CallsBaseConvertTo()
    {
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, 123, typeof(string));

        Assert.Equal("123", result);
    }

    [Fact]
    public void ConvertFrom_WithNullString_ReturnsNull()
    {
        var result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, null!);

        Assert.Null(result);
    }

    [Fact]
    public void ConvertFrom_WithEmptyString_ReturnsNull()
    {
        var result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void CreateInstance_WithAllPropertyValues_ReturnsMatrix()
    {
        var propertyValues = new Hashtable
        {
            ["M11"] = 1f,
            ["M12"] = 2f,
            ["M13"] = 3f,
            ["M14"] = 4f,
            ["M21"] = 5f,
            ["M22"] = 6f,
            ["M23"] = 7f,
            ["M24"] = 8f,
            ["M31"] = 9f,
            ["M32"] = 10f,
            ["M33"] = 11f,
            ["M34"] = 12f,
            ["M41"] = 13f,
            ["M42"] = 14f,
            ["M43"] = 15f,
            ["M44"] = 16f
        };

        var result = converter.CreateInstance(null, propertyValues);

        Assert.NotNull(result);
        var matrix = Assert.IsType<Matrix>(result);
        Assert.Equal(1f, matrix.M11);
        Assert.Equal(2f, matrix.M12);
        Assert.Equal(3f, matrix.M13);
        Assert.Equal(4f, matrix.M14);
        Assert.Equal(5f, matrix.M21);
        Assert.Equal(6f, matrix.M22);
        Assert.Equal(7f, matrix.M23);
        Assert.Equal(8f, matrix.M24);
        Assert.Equal(9f, matrix.M31);
        Assert.Equal(10f, matrix.M32);
        Assert.Equal(11f, matrix.M33);
        Assert.Equal(12f, matrix.M34);
        Assert.Equal(13f, matrix.M41);
        Assert.Equal(14f, matrix.M42);
        Assert.Equal(15f, matrix.M43);
        Assert.Equal(16f, matrix.M44);
    }

    [Fact]
    public void CreateInstance_WithNullPropertyValues_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => converter.CreateInstance(null, null!));
    }

    [Fact]
    public void ConvertTo_IdentityMatrix_ReturnsCorrectString()
    {
        var matrix = Matrix.Identity;

        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, matrix, typeof(string));

        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }
}
