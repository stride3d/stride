
using Xunit;

namespace Stride.Core.Mathematics.Tests
{
    public class TestPoint
    {

        readonly Point testPoint1 = new Point(5,5);
        readonly Point testPoint2 = new Point(10, 10);
        readonly Point testPoint3 = new Point(5, 5);
        
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
