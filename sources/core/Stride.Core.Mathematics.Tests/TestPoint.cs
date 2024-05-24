
using Xunit;

namespace Stride.Core.Mathematics.Tests
{
    public class TestPoint
    {
        Point testPoint1 = new Point(5,5);
        Point testPoint2 = new Point(10, 10);
        Point testPoint3 = new Point(5, 5);
        
        [Fact]
        public void TestPointsNotEqual()
        {
            Assert.NotEqual(testPoint1, testPoint2);
        }

        [Fact]
        public void TestPointsEqual()
        {
            Assert.True(testPoint1.Equals(testPoint3));
        }
    }
}
