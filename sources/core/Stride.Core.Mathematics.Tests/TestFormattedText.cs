// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Numerics;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestFormattedText
{
    [Fact]
    public void WithoutAFormatTheTextIsTheOneOfSystemNumerics()
    {
        var quaternion = new Quaternion(1.5f, -2, 3, 4);
        var plane = new Plane(1.5f, -2, 3, 4);
        var matrix = Matrix4x4.CreateTranslation(1.5f, -2, 3);

        Assert.Equal(quaternion.ToString(), quaternion.ToString(null, CultureInfo.CurrentCulture));
        Assert.Equal(plane.ToString(), plane.ToString(null, CultureInfo.CurrentCulture));
        Assert.Equal(matrix.ToString(), matrix.ToString(null, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TheFormatAndTheCultureApplyToEveryNumber()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Equal("{X:1,50 Y:-2,00 Z:3,00 W:4,00}", new Quaternion(1.5f, -2, 3, 4).ToString("F2", french));
        Assert.Equal($"{{Normal:{new Vector3(1.5f, -2, 3).ToString("F2", french)} D:4,00}}", new Plane(1.5f, -2, 3, 4).ToString("F2", french));

        var matrix = Matrix4x4.CreateTranslation(1.5f, -2, 3).ToString("F2", french);
        Assert.StartsWith("{ {M11:1,00 M12:0,00 M13:0,00 M14:0,00} ", matrix);
        Assert.EndsWith(" {M41:1,50 M42:-2,00 M43:3,00 M44:1,00} }", matrix);
    }
}
