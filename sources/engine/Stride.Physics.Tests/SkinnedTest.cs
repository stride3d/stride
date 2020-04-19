// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xunit;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics.Tests
{
    public class SkinnedTest : GameTest
    {
        public SkinnedTest() : base("SkinnedTest")
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
        public void SkinnedTest1()
        {
            var game = new SkinnedTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                await game.Script.NextFrame();
                await game.Script.NextFrame();

                var character = game.SceneSystem.SceneInstance.RootScene.Entities.First(ent => ent.Name == "Model");
                var dynamicBody = character.GetAll<RigidbodyComponent>().First(x => !x.IsKinematic);
                var kinematicBody = character.GetAll<RigidbodyComponent>().First(x => x.IsKinematic);
                var model = character.Get<ModelComponent>();
                var anim = character.Get<AnimationComponent>();

                var pastTransform = model.Skeleton.NodeTransformations[dynamicBody.BoneIndex].WorldMatrix;

                //let the controller land
                var twoSeconds = 120;
                while (twoSeconds-- > 0)
                {
                    await game.Script.NextFrame();
                }

                Assert.Equal(dynamicBody.BoneWorldMatrix, model.Skeleton.NodeTransformations[dynamicBody.BoneIndex].WorldMatrix);
                Assert.NotEqual(pastTransform, model.Skeleton.NodeTransformations[dynamicBody.BoneIndex].WorldMatrix);

                anim.Play("Run");

                pastTransform = model.Skeleton.NodeTransformations[kinematicBody.BoneIndex].WorldMatrix;

                Assert.Equal(kinematicBody.BoneWorldMatrix, pastTransform);

                await game.Script.NextFrame();

                Assert.NotEqual(kinematicBody.BoneWorldMatrix, pastTransform);

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
