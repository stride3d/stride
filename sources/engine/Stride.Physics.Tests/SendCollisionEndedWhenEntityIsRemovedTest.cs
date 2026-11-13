using System.Linq;
using Stride.Engine;
using Xunit;

namespace Stride.Physics.Tests
{
    public class SendCollisionEndedWhenEntityIsRemovedTest : GameTest
    {
        public SendCollisionEndedWhenEntityIsRemovedTest() : base(nameof(SendCollisionEndedWhenEntityIsRemovedTest))
        {
        }

        [Fact]
        public void WhenEntityIsRemoved_CollisionEndedIsRaised()
        {
            var game = new SendCollisionEndedWhenEntityIsRemovedTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                await game.Script.NextFrame();
                await game.Script.NextFrame();

                var cube = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "Cube");
                var sphere = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "Sphere");

                var cubePhysics = cube.Get<PhysicsComponent>();

                // verify that there is a collision between the sphere and the cube
                Assert.Single(cubePhysics.Collisions);

                // CollisionEnded() only receives from a micro-thread, and only what is sent once it waits:
                // the micro-thread starts below, before the removal
                var collisionEnded = false;
                game.Script.AddTask(async () =>
                {
                    var collision = await cubePhysics.CollisionEnded();
                    Assert.True(collision.HasEndedFromComponentRemoval);
                    Assert.NotNull(collision.ColliderA);
                    Assert.NotNull(collision.ColliderB);
                    collisionEnded = true;
                });

                await game.Script.NextFrame();

                // remove sphere from the scene
                sphere.Scene = null;

                // Physics handles the removal at its next update, and the waiting micro-thread resumes after it
                for (var frame = 0; frame < 10 && !collisionEnded; frame++)
                    await game.Script.NextFrame();

                Assert.True(collisionEnded);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public void WhenBothEntitiesAreRemoved_NoExceptionIsThrown()
        {
            var game = new SendCollisionEndedWhenEntityIsRemovedTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                await game.Script.NextFrame();
                await game.Script.NextFrame();

                var cube = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "Cube");
                var sphere = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "Sphere");

                var cubePhysics = cube.Get<PhysicsComponent>();

                // verify that there is a collision between the sphere and the cube
                Assert.Single(cubePhysics.Collisions);

                // CollisionEnded() only receives from a micro-thread, and only what is sent once it waits:
                // the micro-thread starts below, before the removal
                var collisionEnded = false;
                game.Script.AddTask(async () =>
                {
                    var collision = await cubePhysics.CollisionEnded();
                    Assert.True(collision.HasEndedFromComponentRemoval);
                    Assert.NotNull(collision.ColliderA);
                    Assert.NotNull(collision.ColliderB);
                    collisionEnded = true;
                });

                await game.Script.NextFrame();

                // remove both from the scene
                sphere.Scene = null;
                cube.Scene = null;

                // Physics handles the removal at its next update, and the waiting micro-thread resumes after it
                for (var frame = 0; frame < 10 && !collisionEnded; frame++)
                    await game.Script.NextFrame();

                Assert.True(collisionEnded);

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
