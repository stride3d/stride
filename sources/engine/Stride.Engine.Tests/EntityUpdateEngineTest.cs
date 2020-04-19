// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;
using Stride.Updater;

namespace Stride.Engine.Tests
{
    public class EntityUpdateEngineTest
    {
        [Fact]
        public unsafe void TestComponentAccess()
        {
            var entity = new Entity();

            entity.AddChild(new Entity("child1")
            {
                new LightComponent()
            });

            var modelComponent = new ModelComponent();

            var compiledUpdate = UpdateEngine.Compile(typeof(Entity), new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("[ModelComponent]", 0),
                new UpdateMemberInfo("child1[LightComponent.Key].Intensity", 0), // Keep key just for backward comp, we will remove it
            });

            var testData = new TestData[] { 32.0f };

            fixed (TestData* dataPtr = testData)
            {
                UpdateEngine.Run(entity, compiledUpdate, (IntPtr)dataPtr, new[] { new UpdateObjectData(modelComponent) });
            }

            Assert.Equal(modelComponent, entity.Get<ModelComponent>());
            Assert.Equal(32.0f, entity.GetChild(0).Get<LightComponent>().Intensity);
        }

        struct TestData
        {
            public float Factor;
            public float Value;

            public static implicit operator TestData(float value)
            {
                return new TestData { Factor = 1.0f, Value = value };
            }
        }
    }
}
