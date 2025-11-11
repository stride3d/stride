// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestSphericalHarmonics
{
    [Fact]
    public void TestSphericalHarmonicsConstruction_Order1()
    {
        var sh = new SphericalHarmonics(1);

        Assert.Equal(1, sh.Order);
        Assert.NotNull(sh.Coefficients);
        Assert.Single(sh.Coefficients); // order^2 = 1
    }

    [Fact]
    public void TestSphericalHarmonicsConstruction_Order2()
    {
        var sh = new SphericalHarmonics(2);

        Assert.Equal(2, sh.Order);
        Assert.Equal(4, sh.Coefficients.Length); // order^2 = 4
    }

    [Fact]
    public void TestSphericalHarmonicsConstruction_Order3()
    {
        var sh = new SphericalHarmonics(3);

        Assert.Equal(3, sh.Order);
        Assert.Equal(9, sh.Coefficients.Length); // order^2 = 9
    }

    [Fact]
    public void TestSphericalHarmonicsConstruction_Order4()
    {
        var sh = new SphericalHarmonics(4);

        Assert.Equal(4, sh.Order);
        Assert.Equal(16, sh.Coefficients.Length); // order^2 = 16
    }

    [Fact]
    public void TestSphericalHarmonicsConstruction_Order5()
    {
        var sh = new SphericalHarmonics(5);

        Assert.Equal(5, sh.Order);
        Assert.Equal(25, sh.Coefficients.Length); // order^2 = 25
    }

    [Fact]
    public void TestSphericalHarmonicsIndexer_GetSet()
    {
        var sh = new SphericalHarmonics(3);
        var testColor = new Color3(1.0f, 0.5f, 0.25f);

        sh[1, 0] = testColor;
        var result = sh[1, 0];

        Assert.Equal(testColor, result);
    }

    [Fact]
    public void TestSphericalHarmonicsIndexer_MultipleValues()
    {
        var sh = new SphericalHarmonics(3);

        sh[0, 0] = new Color3(1.0f, 0.0f, 0.0f);
        sh[1, -1] = new Color3(0.0f, 1.0f, 0.0f);
        sh[1, 0] = new Color3(0.0f, 0.0f, 1.0f);
        sh[1, 1] = new Color3(1.0f, 1.0f, 0.0f);

        Assert.Equal(new Color3(1.0f, 0.0f, 0.0f), sh[0, 0]);
        Assert.Equal(new Color3(0.0f, 1.0f, 0.0f), sh[1, -1]);
        Assert.Equal(new Color3(0.0f, 0.0f, 1.0f), sh[1, 0]);
        Assert.Equal(new Color3(1.0f, 1.0f, 0.0f), sh[1, 1]);
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_Order1()
    {
        var sh = new SphericalHarmonics(1);
        sh[0, 0] = new Color3(1.0f, 0.5f, 0.25f);

        var direction = Vector3.UnitX;
        var result = sh.Evaluate(direction);

        // Result should be non-zero (actual value depends on SH math)
        Assert.True(result.R > 0 || result.G > 0 || result.B > 0);
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_Order2()
    {
        var sh = new SphericalHarmonics(2);

        // Set some positive coefficients
        for (int l = 0; l < 2; l++)
        {
            for (int m = -l; m <= l; m++)
            {
                sh[l, m] = new Color3(0.5f, 0.5f, 0.5f);
            }
        }

        var direction = new Vector3(1, 1, 0);
        direction.Normalize();
        var result = sh.Evaluate(direction);

        // Verify result is valid (SH can produce negative or positive values)
        Assert.True(!float.IsNaN(result.R));
        Assert.True(!float.IsNaN(result.G));
        Assert.True(!float.IsNaN(result.B));
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_Order3()
    {
        var sh = new SphericalHarmonics(3);

        // Set white light coefficient
        sh[0, 0] = new Color3(1.0f, 1.0f, 1.0f);

        var direction = Vector3.UnitY;
        var result = sh.Evaluate(direction);

        Assert.True(result.R > 0);
        Assert.True(result.G > 0);
        Assert.True(result.B > 0);
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_DifferentDirections()
    {
        var sh = new SphericalHarmonics(2);
        sh[0, 0] = new Color3(1.0f, 1.0f, 1.0f);

        var result1 = sh.Evaluate(Vector3.UnitX);
        var result2 = sh.Evaluate(Vector3.UnitY);
        var result3 = sh.Evaluate(Vector3.UnitZ);

        // All should produce valid color values
        Assert.True(result1.R > 0);
        Assert.True(result2.G > 0);
        Assert.True(result3.B > 0);
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_ZeroCoefficients()
    {
        var sh = new SphericalHarmonics(2);

        // Leave all coefficients at default (zero)
        var direction = Vector3.UnitX;
        var result = sh.Evaluate(direction);

        Assert.Equal(0.0f, result.R);
        Assert.Equal(0.0f, result.G);
        Assert.Equal(0.0f, result.B);
    }

    [Fact]
    public void TestSphericalHarmonicsBaseCoefficients_NotNull()
    {
        Assert.NotNull(SphericalHarmonics.BaseCoefficients);
        Assert.True(SphericalHarmonics.BaseCoefficients.Length > 0);
    }

    [Fact]
    public void TestSphericalHarmonicsBaseCoefficients_Count()
    {
        // BaseCoefficients should have 25 elements (for order 5: 5^2 = 25)
        Assert.Equal(25, SphericalHarmonics.BaseCoefficients.Length);
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_Order4()
    {
        var sh = new SphericalHarmonics(4);

        // Set some red light
        sh[0, 0] = new Color3(1.0f, 0.0f, 0.0f);
        sh[1, 0] = new Color3(0.5f, 0.0f, 0.0f);

        var direction = new Vector3(0.5f, 0.5f, 0.5f);
        direction.Normalize();
        var result = sh.Evaluate(direction);

        // Should have some red component
        Assert.True(result.R > 0);
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_Order5()
    {
        var sh = new SphericalHarmonics(5);

        // Set blue light
        sh[0, 0] = new Color3(0.0f, 0.0f, 1.0f);

        var direction = Vector3.UnitZ;
        var result = sh.Evaluate(direction);

        // Should have blue component
        Assert.True(result.B > 0);
    }

    [Fact]
    public void TestSphericalHarmonicsEvaluate_NegativeDirection()
    {
        var sh = new SphericalHarmonics(2);
        sh[0, 0] = new Color3(1.0f, 1.0f, 1.0f);

        var direction = -Vector3.UnitX;
        var result = sh.Evaluate(direction);

        // Should still produce valid result
        Assert.True(result.R > 0 || result.G > 0 || result.B > 0);
    }

    [Fact]
    public void TestSphericalHarmonicsIndexer_BoundaryL()
    {
        var sh = new SphericalHarmonics(3);

        // Test boundary: l = order - 1 = 2
        sh[2, 0] = new Color3(1.0f, 0.0f, 0.0f);
        var result = sh[2, 0];

        Assert.Equal(new Color3(1.0f, 0.0f, 0.0f), result);
    }

    [Fact]
    public void TestSphericalHarmonicsIndexer_BoundaryM()
    {
        var sh = new SphericalHarmonics(3);

        // Test boundary: m = -l and m = +l
        sh[2, -2] = new Color3(1.0f, 0.0f, 0.0f);
        sh[2, 2] = new Color3(0.0f, 1.0f, 0.0f);

        Assert.Equal(new Color3(1.0f, 0.0f, 0.0f), sh[2, -2]);
        Assert.Equal(new Color3(0.0f, 1.0f, 0.0f), sh[2, 2]);
    }

    [Fact]
    public void TestSphericalHarmonicsCoefficientsLength()
    {
        var sh1 = new SphericalHarmonics(1);
        Assert.Single(sh1.Coefficients); // 1*1

        var sh2 = new SphericalHarmonics(2);
        Assert.Equal(4, sh2.Coefficients.Length); // 2*2

        var sh3 = new SphericalHarmonics(3);
        Assert.Equal(9, sh3.Coefficients.Length); // 3*3
    }

    [Fact]
    public void TestSphericalHarmonicsOrderProperty()
    {
        var sh = new SphericalHarmonics(3);
        Assert.Equal(3, sh.Order);
    }
}

