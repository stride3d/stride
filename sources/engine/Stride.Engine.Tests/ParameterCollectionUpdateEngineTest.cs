// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Rendering;
using Stride.Updater;

namespace Stride.Engine.Tests
{
    public class ParameterCollectionUpdateEngineTest
    {
        public static readonly ValueParameterKey<int> IntKey = ParameterKeys.NewValue<int>();
        public static readonly ValueParameterKey<BlittableStruct> BlittableKey = ParameterKeys.NewValue<BlittableStruct>();
        public static readonly ObjectParameterKey<object> ObjectKey = ParameterKeys.NewObject<object>();

        [Fact]
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

            Assert.Equal(123, test.Parameters.Get(IntKey));
            Assert.Equal(123, test.Parameters.Get(BlittableKey).IntField);
            Assert.Equal(123, test.Parameters.Get(BlittableKey).IntProperty);
            Assert.Equal(test2, test.Parameters.Get(ObjectKey));
        }
    }

    [DataContract]
    public class TestParameterCollectionClass
    {
        public ParameterCollection Parameters = new ParameterCollection();
    }

}
