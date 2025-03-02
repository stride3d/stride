
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestPoint
{
    readonly Point testPoint1 = new(5, 5);
    readonly Point testPoint2 = new(10, 10);
    readonly Point testPoint3 = new(5, 5);

    [Fact]
    public void TestPointsNotEqual()
    {
        Assert.NotEqual(testPoint1, testPoint2);
    }

    [Fact]
    public void TestPointsEqual()
    {
        Assert.Equal(testPoint1, testPoint3);
    }
}
