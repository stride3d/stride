// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;
using Stride.Core.Reflection;
using System.Collections.Generic;

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

        // IdentifierHelper is now obsolete
        //[Fact]
        //public void TestIdentifierHelper()
        //{
        //    // Has IdentifierHelper is using ShadowObject, we will test it here
        //    ShadowObject.Enable = true;
        //    var obj = new object();

        //    var id = IdentifiableHelper.GetId(obj);
        //    Assert.NotEqual(Guid.Empty, id);

        //    var id1 = IdentifiableHelper.GetId(obj);
        //    Assert.Equal(id, id1);

        //    // We should not get an id for a collection
        //    var idCollection = IdentifiableHelper.GetId(new List<object>());
        //    Assert.Equal(Guid.Empty, idCollection);

        //    // We should not get an id for a dictionary
        //    var idDict = IdentifiableHelper.GetId(new MyDictionary());
        //    Assert.Equal(Guid.Empty, idDict);
        //}

        private class MyDictionary : Dictionary<object, object>
        {
        }
    }
}
