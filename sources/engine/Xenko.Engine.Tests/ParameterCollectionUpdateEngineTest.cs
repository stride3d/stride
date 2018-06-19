// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Xenko.Core;
using Xenko.Engine.Design;
using Xenko.Rendering;
using Xenko.Updater;

namespace Xenko.Engine.Tests
{
    [TestFixture]
    public class ParameterCollectionUpdateEngineTest
    {
        public static readonly ValueParameterKey<int> IntKey = ParameterKeys.NewValue<int>();
        public static readonly ValueParameterKey<BlittableStruct> BlittableKey = ParameterKeys.NewValue<BlittableStruct>();
        public static readonly ObjectParameterKey<object> ObjectKey = ParameterKeys.NewObject<object>();

        [Test]
        public void TestParameterCollectionResolver()
        {
            var test = new TestParameterCollectionClass();
            var test2 = new object();

            UpdateEngine.RegisterMemberResolver(new ParameterCollectionResolver());

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo($"Parameters[{nameof(ParameterCollectionUpdateEngineTest)}.{nameof(IntKey)}]", 0),
                new UpdateMemberInfo($"Parameters[{nameof(ParameterCollectionUpdateEngineTest)}.{nameof(BlittableKey)}].IntField", 0),
                new UpdateMemberInfo($"Parameters[{nameof(ParameterCollectionUpdateEngineTest)}.{nameof(BlittableKey)}].IntProperty", 0),
                new UpdateMemberInfo($"Parameters[{nameof(ParameterCollectionUpdateEngineTest)}.{nameof(ObjectKey)}]", 0),
            };

            var blittableData = new TestUpdateEngine.TestData[] { 123, 456 };
            var objectData = new[] { new UpdateObjectData(test2) };

            TestUpdateEngine.RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.Parameters.Get(IntKey), Is.EqualTo(123));
            Assert.That(test.Parameters.Get(BlittableKey).IntField, Is.EqualTo(123));
            Assert.That(test.Parameters.Get(BlittableKey).IntProperty, Is.EqualTo(123));
            Assert.That(test.Parameters.Get(ObjectKey), Is.EqualTo(test2));
        }
    }

    [DataContract]
    public class TestParameterCollectionClass
    {
        public ParameterCollection Parameters = new ParameterCollection();
    }

}
