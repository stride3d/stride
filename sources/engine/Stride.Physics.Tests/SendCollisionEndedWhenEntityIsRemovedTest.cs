using System.Linq;
using System.Threading.Tasks;
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

                var collisionEndedTask = Task.Run(async () => {
                    var collision = await cubePhysics.CollisionEnded();
                    // when we receive the collision it's actually destroyed already
                    Assert.Null(collision.ColliderA);
                    Assert.Null(collision.ColliderB);
                });

                await game.Script.NextFrame();

                // remove sphere from the scene
                sphere.Scene = null;

                await game.Script.NextFrame();

                Assert.True(collisionEndedTask.IsCompleted);

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

                var collisionEndedTask = Task.Run(async () => {
                    var collision = await cubePhysics.CollisionEnded();
                    Assert.Null(collision.ColliderA);
                    Assert.Null(collision.ColliderB);
                });

                await game.Script.NextFrame();

                // remove both from the scene
                sphere.Scene = null;
                cube.Scene = null;

                await game.Script.NextFrame();

                Assert.True(collisionEndedTask.IsCompleted);

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
