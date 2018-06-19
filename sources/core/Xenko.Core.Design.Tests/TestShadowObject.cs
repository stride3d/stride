// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using NUnit.Framework;
using Xenko.Core.Reflection;
using System.Collections.Generic;

namespace Xenko.Core.Design.Tests
{
    [TestFixture]
    public class TestShadowObject
    {
        [Test]
        public void TestGetAndGetOrCreate()
        {
            ShadowObject.Enable = true;
            var obj = new object();

            var shadowObject = ShadowObject.Get(obj);
            Assert.Null(shadowObject);

            shadowObject = ShadowObject.GetOrCreate(obj);
            Assert.NotNull(shadowObject);

            var shadowObject2 = ShadowObject.GetOrCreate(obj);
            Assert.AreEqual(shadowObject, shadowObject2);
        }

        // IdentifierHelper is now obsolete
        //[Test]
        //public void TestIdentifierHelper()
        //{
        //    // Has IdentifierHelper is using ShadowObject, we will test it here
        //    ShadowObject.Enable = true;
        //    var obj = new object();

        //    var id = IdentifiableHelper.GetId(obj);
        //    Assert.AreNotEqual(Guid.Empty, id);

        //    var id1 = IdentifiableHelper.GetId(obj);
        //    Assert.AreEqual(id, id1);

        //    // We should not get an id for a collection
        //    var idCollection = IdentifiableHelper.GetId(new List<object>());
        //    Assert.AreEqual(Guid.Empty, idCollection);

        //    // We should not get an id for a dictionary
        //    var idDict = IdentifiableHelper.GetId(new MyDictionary());
        //    Assert.AreEqual(Guid.Empty, idDict);
        //}

        private class MyDictionary : Dictionary<object, object>
        {
        }
    }
}
