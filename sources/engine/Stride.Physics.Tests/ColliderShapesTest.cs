// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xunit;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics.Tests
{
    public class ColliderShapesTest : GameTest
    {
        public ColliderShapesTest() : base("ColliderShapesTest")
        {
        }

        public static bool ScreenPositionToWorldPositionRaycast(Vector2 screenPos, CameraComponent camera, Simulation simulation)
        {
            var invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

            Vector3 sPos;
            sPos.X = screenPos.X * 2f - 1f;
            sPos.Y = 1f - screenPos.Y * 2f;

            sPos.Z = 0f;
            var vectorNear = Vector3.Transform(sPos, invViewProj);
            vectorNear /= vectorNear.W;

            sPos.Z = 1f;
            var vectorFar = Vector3.Transform(sPos, invViewProj);
            vectorFar /= vectorFar.W;

            var result = new FastList<HitResult>();
            simulation.RaycastPenetrating(vectorNear.XYZ(), vectorFar.XYZ(), result);
            foreach (var hitResult in result)
            {
                if (hitResult.Succeeded)
                {
                    return true;
                }                
            }

            return false;
        }

        [Fact]
        public void ColliderShapesTest1()
        {
            var game = new ColliderShapesTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                await game.Script.NextFrame();
                await game.Script.NextFrame();
                var simulation = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "Simulation").Get<StaticColliderComponent>().Simulation;

                HitResult hit;

                var cube = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "CubePrefab1");

                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.55f, 0.0f), cube.Transform.Position + new Vector3(0.0f, 0.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.5f, 0.0f), cube.Transform.Position + new Vector3(0.0f, 0.5f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(-Vector3.UnitZ, hit.Normal);
                Assert.Equal(hit.Point, cube.Transform.Position + new Vector3(0.0f, 0.5f, -0.5f));

                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.55f, 0.0f), cube.Transform.Position + new Vector3(0.0f, -0.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.5f, 0.0f), cube.Transform.Position + new Vector3(0.0f, -0.5f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(-Vector3.UnitZ, hit.Normal);
                Assert.Equal(hit.Point, cube.Transform.Position + new Vector3(0.0f, -0.5f, -0.5f));

                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.55f, 0.0f, 0.0f), cube.Transform.Position + new Vector3(0.55f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.5f, 0.0f, 0.0f), cube.Transform.Position + new Vector3(0.5f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(-Vector3.UnitZ, hit.Normal);
                Assert.Equal(hit.Point, cube.Transform.Position + new Vector3(0.5f, 0.0f, -0.5f));

                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.55f, 0.0f, 0.0f), cube.Transform.Position + new Vector3(-0.55f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cube.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.5f, 0.0f, 0.0f), cube.Transform.Position + new Vector3(-0.5f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(-Vector3.UnitZ, hit.Normal);
                Assert.Equal(hit.Point, cube.Transform.Position + new Vector3(-0.5f, 0.0f, -0.5f));

                var cylinder = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "CylinderPrefab1");

                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.55f, 0.0f), cylinder.Transform.Position + new Vector3(0.0f, 0.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.5f, 0.0f), cylinder.Transform.Position + new Vector3(0.0f, 0.5f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0, 0.9991773f, -0.04055634f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(2.17587972f, 0.5f, -7.46081161f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.55f, 0.0f), cylinder.Transform.Position + new Vector3(0.0f, -0.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.5f, 0.0f), cylinder.Transform.Position + new Vector3(0.0f, -0.5f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0, -0.9999594f, -0.00901306048f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(2.17587972f, -0.5f, -7.460182f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.55f, 0.0f, 0.0f), cylinder.Transform.Position + new Vector3(0.55f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.5f, 0.0f, 0.0f), cylinder.Transform.Position + new Vector3(0.5f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0.984618843f, -0.00194456265f, -0.174706012f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(2.67587972f, 0, -7.049136f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.55f, 0.0f, 0.0f), cylinder.Transform.Position + new Vector3(-0.55f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cylinder.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.5f, 0.0f, 0.0f), cylinder.Transform.Position + new Vector3(-0.5f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(-0.984617054f, -0.00194378383f, -0.174715757f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(1.67587972f, 0, -7.049135f) - hit.Point).Length(), 3);

                var capsule = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "CapsulePrefab1");

                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.65f, 0.0f), capsule.Transform.Position + new Vector3(0.0f, 0.65f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.6f, 0.0f), capsule.Transform.Position + new Vector3(0.0f, 0.6f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0, 0.9758787f, -0.218313679f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(0, 1.5999999f, -7.03867149f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.65f, 0.0f), capsule.Transform.Position + new Vector3(0.0f, -0.65f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.6f, 0.0f), capsule.Transform.Position + new Vector3(0.0f, -0.6f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0, -0.999195457f, -0.0401064046f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(0, 0.399999917f, -7.007019f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.40f, 0.0f, 0.0f), capsule.Transform.Position + new Vector3(0.40f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.35f, 0.0f, 0.0f), capsule.Transform.Position + new Vector3(0.35f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0.9830295f, -0.0126968855f, -0.1830078f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(0.35f, 0.99999994f, -7.049801f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.40f, 0.0f, 0.0f), capsule.Transform.Position + new Vector3(-0.40f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((capsule.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.35f, 0.0f, 0.0f), capsule.Transform.Position + new Vector3(-0.35f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(-0.9830295f, -0.0126968855f, -0.1830078f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-0.35f, 0.99999994f, -7.049801f) - hit.Point).Length(), 3);

                var cone = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "ConePrefab1");

                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.55f, 0.0f), cone.Transform.Position + new Vector3(0.0f, 0.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 0.5f, 0.0f), cone.Transform.Position + new Vector3(0.0f, 0.5f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0, 0.5078509f, -0.8614451f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(1, 0.5f, -12.04643f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.55f, 0.0f), cone.Transform.Position + new Vector3(0.0f, -0.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.5f, 0.0f), cone.Transform.Position + new Vector3(0.0f, -0.5f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0, 0, -1) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(1, -0.5f, -12.54f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.35f, 0.0f, 0.0f), cone.Transform.Position + new Vector3(0.35f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.3f, 0.0f, 0.0f), cone.Transform.Position + new Vector3(0.3f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0.8664685f, 0.4454662f, -0.2253714f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(1.3f, 0, -12.02208f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.35f, 0.0f, 0.0f), cone.Transform.Position + new Vector3(-0.35f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((cone.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.3f, 0.0f, 0.0f), cone.Transform.Position + new Vector3(-0.3f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(-0.8664652f, 0.4454676f, -0.2253817f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(0.7f, 0, -12.02208f) - hit.Point).Length(), 3);

                var compound1 = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "Compound1");

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 1.55f, 0.0f), compound1.Transform.Position + new Vector3(0.0f, 1.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 1.49f, 0.0f), compound1.Transform.Position + new Vector3(0.0f, 1.49f, 0.0f)); //compound margin is different
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0f, -1.146684E-06f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-3.866335f, 1.407022f, -17.4267f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.55f, 0.0f), compound1.Transform.Position + new Vector3(0.0f, -0.55f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.5f, 0.0f), compound1.Transform.Position + new Vector3(0.0f, -0.5f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0f, 0f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-3.866335f, -0.5829783f, -17.4267f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(1.55f, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(1.55f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(1.49f, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(1.49f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0f, 7.166773E-08f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-2.376335f, -0.08297831f, -17.4267f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.55f, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(-0.55f, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.5f, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(-0.5f, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0f, 0f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-4.366335f, -0.08297831f, -17.4267f) - hit.Point).Length(), 3);

                var scaling = new Vector3(3, 2, 2);

                compound1.Transform.Scale = scaling;
                compound1.Transform.UpdateWorldMatrix();
                compound1.Get<PhysicsComponent>().UpdatePhysicsTransformation();

                await game.Script.NextFrame();

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 1.55f * 2, 0.0f), compound1.Transform.Position + new Vector3(0.0f, 1.55f * 2, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, 1.49f * 2, 0.0f), compound1.Transform.Position + new Vector3(0.0f, 1.49f * 2, 0.0f)); //compound margin is different
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(3.12393E-07f, 0f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-3.866335f, 2.897022f, -17.9267f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.55f * 2, 0.0f), compound1.Transform.Position + new Vector3(0.0f, -0.55f * 2, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(0.0f, -0.5f * 2, 0.0f), compound1.Transform.Position + new Vector3(0.0f, -0.5f * 2, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0f, 0f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-3.866335f, -1.082978f, -17.9267f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(1.55f * 3, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(1.55f * 3, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(1.49f * 3, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(1.49f * 3, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(0f, 0f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(0.6036654f, -0.08297831f, -17.9267f) - hit.Point).Length(), 3);

                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.55f * 3, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(-0.55f * 3, 0.0f, 0.0f));
                Assert.False(hit.Succeeded);
                hit = simulation.Raycast((compound1.Transform.Position - Vector3.UnitZ * 2) + new Vector3(-0.5f * 3, 0.0f, 0.0f), compound1.Transform.Position + new Vector3(-0.5f * 3, 0.0f, 0.0f));
                Assert.True(hit.Succeeded);
                Assert.Equal(0.0f, (new Vector3(-2.861034E-06f, 3.889218E-06f, -1f) - hit.Normal).Length(), 3);
                Assert.Equal(0.0f, (new Vector3(-5.366335f, -0.08297831f, -17.9267f) - hit.Point).Length(), 3);

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
