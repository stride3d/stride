// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Yaml.Tests;

public class TestMark
{
    [Fact]
    public void TestMarkDefaultValues()
    {
        var mark = new Mark();

        Assert.Equal(0, mark.Index);
        Assert.Equal(0, mark.Line);
        Assert.Equal(0, mark.Column);
    }

    [Fact]
    public void TestMarkSetProperties()
    {
        var mark = new Mark
        {
            Index = 10,
            Line = 5,
            Column = 3
        };

        Assert.Equal(10, mark.Index);
        Assert.Equal(5, mark.Line);
        Assert.Equal(3, mark.Column);
    }

    [Fact]
    public void TestMarkIndexNegativeThrows()
    {
        var mark = new Mark();

        Assert.Throws<ArgumentOutOfRangeException>(() => mark.Index = -1);
    }

    [Fact]
    public void TestMarkLineNegativeThrows()
    {
        var mark = new Mark();

        Assert.Throws<ArgumentOutOfRangeException>(() => mark.Line = -1);
    }

    [Fact]
    public void TestMarkColumnNegativeThrows()
    {
        var mark = new Mark();

        Assert.Throws<ArgumentOutOfRangeException>(() => mark.Column = -1);
    }

    [Fact]
    public void TestMarkZeroValuesAreValid()
    {
        var mark = new Mark
        {
            Index = 0,
            Line = 0,
            Column = 0
        };

        Assert.Equal(0, mark.Index);
        Assert.Equal(0, mark.Line);
        Assert.Equal(0, mark.Column);
    }

    [Fact]
    public void TestMarkToString()
    {
        var mark = new Mark
        {
            Index = 100,
            Line = 10,
            Column = 5
        };

        var result = mark.ToString();

        Assert.Contains("Lin: 10", result);
        Assert.Contains("Col: 5", result);
        Assert.Contains("Chr: 100", result);
    }

    [Fact]
    public void TestMarkToStringWithZeroValues()
    {
        var mark = new Mark();

        var result = mark.ToString();

        Assert.Contains("Lin: 0", result);
        Assert.Contains("Col: 0", result);
        Assert.Contains("Chr: 0", result);
    }

    [Fact]
    public void TestMarkEmpty()
    {
        var empty = Mark.Empty;

        Assert.Equal(0, empty.Index);
        Assert.Equal(0, empty.Line);
        Assert.Equal(0, empty.Column);
    }

    [Fact]
    public void TestMarkLargeValues()
    {
        var mark = new Mark
        {
            Index = int.MaxValue,
            Line = int.MaxValue,
            Column = int.MaxValue
        };

        Assert.Equal(int.MaxValue, mark.Index);
        Assert.Equal(int.MaxValue, mark.Line);
        Assert.Equal(int.MaxValue, mark.Column);
    }

    [Fact]
    public void TestMarkStructValueSemantics()
    {
        var mark1 = new Mark { Index = 10, Line = 5, Column = 3 };
        var mark2 = mark1; // Copy

        mark2.Index = 20;

        // Original should be unchanged (value semantics)
        Assert.Equal(10, mark1.Index);
        Assert.Equal(20, mark2.Index);
    }
}
