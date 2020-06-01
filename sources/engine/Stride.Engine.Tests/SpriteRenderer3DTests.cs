// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering.Sprites;

namespace Stride.Engine.Tests
{
    public class SpriteRenderer3DTests : EngineTestBase
    {
        private float currentTime;

        private readonly List<Entity> rotatingSprites = new List<Entity>();
        private readonly List<Entity> entities = new List<Entity>();
        private TestCamera cameraScript;
        private Entity ratio;

        private const int ScreenWidth = 1024;
        private const int ScreenHeight = 780;

        public SpriteRenderer3DTests()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = ScreenWidth;
            GraphicsDeviceManager.PreferredBackBufferHeight = ScreenHeight;
        }

        private Entity CreateSpriteEntity(SpriteSheet sheet, string frameName, bool addToScene = true)
        {
            var entity = new Entity(frameName)
            {
                new SpriteComponent
                {
                    SpriteProvider = SpriteFromSheet.Create(sheet, frameName)
                }
            };

            if (addToScene)
                entities.Add(entity);

            return entity;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var debugSheet = Content.Load<SpriteSheet>("DebugSpriteSheet");

            // normal reference one
            var normal = CreateSpriteEntity(debugSheet, "Normal");
            normal.Transform.Position = new Vector3(75, 75, 0);
            normal.Transform.RotationEulerXYZ = new Vector3(0.8f, 1.2f, 0.3f);
            normal.Get<SpriteComponent>().CurrentSprite.Center = new Vector2(75);

            // billboard
            var billboard = CreateSpriteEntity(debugSheet, "Billboard");
            billboard.Transform.Position = new Vector3(150, 150, 0);
            billboard.Transform.RotationEulerXYZ = new Vector3(0.5f, 1f, 1.5f);
            billboard.Get<SpriteComponent>().SpriteType = SpriteType.Billboard;

            // ratio
            var child = CreateSpriteEntity(debugSheet, "Child", false);
            child.Transform.Position = new Vector3(100, 0, 0);
            ratio = CreateSpriteEntity(debugSheet, "OtherRatio");
            ratio.Transform.Position = new Vector3(350, 150, 0);
            ratio.AddChild(child);

            // depth test
            var onBack = CreateSpriteEntity(debugSheet, "OnBack");
            onBack.Transform.Position = new Vector3(75, 250, 0);
            onBack.Transform.RotationEulerXYZ = new Vector3(0, 1f, 0);
            onBack.Get<SpriteComponent>().CurrentSprite.Center = new Vector2(75);
            var onFront = CreateSpriteEntity(debugSheet, "OnFront");
            onFront.Transform.Position = new Vector3(75, 350, 0.1f);
            onFront.Transform.RotationEulerXYZ = new Vector3(0, 1f, 0);
            onFront.Get<SpriteComponent>().CurrentSprite.Center = new Vector2(75);
            var noDepth = CreateSpriteEntity(debugSheet, "NoDepth");
            noDepth.Transform.Position = new Vector3(75, 450, 0.2f);
            noDepth.Get<SpriteComponent>().CurrentSprite.Center = new Vector2(75);
            noDepth.Get<SpriteComponent>().IgnoreDepth = true;

            // create the rotating sprites
            rotatingSprites.Add(CreateSpriteEntity(debugSheet, "Center"));
            rotatingSprites.Add(CreateSpriteEntity(debugSheet, "TopLeft"));
            rotatingSprites.Add(CreateSpriteEntity(debugSheet, "OutOfImage"));

            for (int i = 0; i < rotatingSprites.Count; i++)
            {
                // Add some z-offset, so non-deterministic sorting due to threading does not cause issues with blending and depth testing enabled
                rotatingSprites[i].Transform.Position = new Vector3(ScreenWidth, ScreenHeight, 0) / 2;
            }

            // add all the entities to the scene
            foreach (var entity in entities)
                SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

            cameraScript = new TestCamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor);
            CameraComponent = cameraScript.Camera;
            Script.Add(cameraScript);

            cameraScript.Yaw = 0;
            cameraScript.Pitch = 0;
            cameraScript.Position = new Vector3(400, 300, 800);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(() => UpdateSprites(0.6f)).TakeScreenshot();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            const float speed = 1 / 60f;
            currentTime += speed;

            cameraScript.Camera.NearClipPlane = 1f;
            cameraScript.Camera.FarClipPlane = 10000f;

            if (!ScreenShotAutomationEnabled)
                UpdateSprites(currentTime);
        }

        private void UpdateSprites(float time)
        {
            for (int i = 0; i < rotatingSprites.Count; i++)
            {
                var rotation = Vector3.Zero;
                rotation[i] = time;
                rotatingSprites[i].Transform.RotationEulerXYZ = rotation;
            }

            ratio.Transform.Scale = new Vector3(time % 5f) / 5;
        }

        [Fact]
        public void SpriteRender3DRun()
        {
            RunGameTest(new SpriteRenderer3DTests());
        }
    }
}
