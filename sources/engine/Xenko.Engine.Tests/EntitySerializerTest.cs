// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using NUnit.Framework;
using Xenko.Core.Mathematics;
using Xenko.Graphics.Regression;

namespace Xenko.Engine.Tests
{
    class EntitySerializerTest : GameTestBase
    {
        [Test]
        public void TestSaveAndLoadEntities()
        {
            PerformTest(game =>
            {
                var entity = new Entity { Transform = { Position = new Vector3(100.0f, 0.0f, 0.0f) } };
                game.Content.Save("EntityAssets/Entity", entity);

                GC.Collect();

                var entity2 = game.Content.Load<Entity>("EntityAssets/Entity");
                Assert.AreEqual(entity.Transform.Position, entity2.Transform.Position);
            });
        }
    }
}
