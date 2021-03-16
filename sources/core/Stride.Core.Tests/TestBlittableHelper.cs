using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Stride.Core.Tests
{
    public class TestBlittableHelper
    {
        private class NonBlittableClass
        {
            public int TestProperty { get; set; }
        }

        private struct BlittableStruct
        {
            public int TestProperty { get; set; }
        }

        private struct NonBlittableStruct
        {
            public NonBlittableClass TestProperty { get; set; }
        }

        [Fact]
        public void TestBlittableTypes()
        {
            Assert.True(BlittableHelper.IsBlittable(typeof(int)));
            Assert.True(BlittableHelper.IsBlittable(typeof(BlittableStruct)));
        }

        [Fact]
        public void TestNotBlittableTypes()
        {
            Assert.False(BlittableHelper.IsBlittable(typeof(NonBlittableStruct)));
            Assert.False(BlittableHelper.IsBlittable(typeof(NonBlittableClass)));
        }
    }
}
