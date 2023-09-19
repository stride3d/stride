// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Reflection;

namespace Stride.Core.Design.Tests
{
    public class TestShadowObject
    {
        [Fact]
        public void TestGetAndGetOrCreate()
        {
            ShadowObject.Enable = true;
            var obj = new object();

            var shadowObject = ShadowObject.Get(obj);
            Assert.Null(shadowObject);

            shadowObject = ShadowObject.GetOrCreate(obj);
            Assert.NotNull(shadowObject);

            var shadowObject2 = ShadowObject.GetOrCreate(obj);
            Assert.Equal(shadowObject, shadowObject2);
        }
    }
}
