// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;
using Xenko.Core.Mathematics;
using Xenko.Graphics.Regression;

namespace Xenko.Engine.Tests
{
    public class EntitySerializerTest : GameTestBase
    {
        [Fact]
        public void TestSaveAndLoadEntities()
        {
            PerformTest(game =>
            {
                var entity = new Entity { Transform = { Position = new Vector3(100.0f, 0.0f, 0.0f) } };
                game.Content.Save("EntityAssets/Entity", entity);

                GC.Collect();

                var entity2 = game.Content.Load<Entity>("EntityAssets/Entity");
                Assert.Equal(entity.Transform.Position, entity2.Transform.Position);
            });
        }
    }
}
