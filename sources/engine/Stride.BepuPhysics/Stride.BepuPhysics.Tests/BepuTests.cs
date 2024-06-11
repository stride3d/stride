// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Colliders;
using Xunit;
using Stride.Engine;
using Stride.Graphics.Regression;

namespace Stride.BepuPhysics.Tests
{
    public class BepuTests : GameTestBase
    {
        [Fact]
        public static void ConstraintsTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                var e1 = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var e2 = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var e3 = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var c = new HingeConstraintComponent { A = e1, B = e2 };

                Assert.False(c.Attached);

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new EntityComponent[]{ e1, e2, e3, c }.Select(x => new Entity{ x }));

                Assert.True(c.Attached);

                e1.SimulationIndex = 1;

                Assert.False(c.Attached);

                e1.SimulationIndex = 0;

                Assert.True(c.Attached);

                e1.Entity.Scene = null;

                Assert.False(c.Attached);

                c.A = e3;

                Assert.True(c.Attached);

                ((BoxCollider)((CompoundCollider)e3.Collider).Colliders[0]).Mass *= 2f;

                Assert.True(c.Attached);

                c.A = null;

                Assert.False(c.Attached);

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
