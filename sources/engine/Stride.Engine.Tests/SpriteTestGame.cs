// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering.Sprites;

namespace Stride.Engine.Tests
{
    public class SpriteTestGame : EngineTestBase
    {
        private SpriteSheet ballSprite1;
        private SpriteSheet ballSprite2;

        private Entity ball;

        private SpriteComponent spriteComponent;

        private Vector2 areaSize;

        private TransformComponent transformComponent;

        private Vector2 ballSpeed = new Vector2(-300, -200);

        private Entity foreground;

        private Entity background;

        private SpriteSheet groundSprites;

        public SpriteTestGame()
        {
            GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_9_1 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            
            // sets the virtual resolution
            areaSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);

            // Creates the camera
            CameraComponent.UseCustomProjectionMatrix = true;
            CameraComponent.ProjectionMatrix = Matrix.OrthoRH(areaSize.X, areaSize.Y, -2, 2);

            // Load assets
            groundSprites = Content.Load<SpriteSheet>("GroundSprite");
            ballSprite1 = Content.Load<SpriteSheet>("BallSprite1");
            ballSprite2 = Content.Load<SpriteSheet>("BallSprite2");
            ball = new Entity { new SpriteComponent { SpriteProvider = new SpriteFromSheet { Sheet = Content.Load<SpriteSheet>("BallSprite1") } } };

            // create fore/background entities
            foreground = new Entity();
            background = new Entity();
            foreground.Add(new SpriteComponent { SpriteProvider = new SpriteFromSheet { Sheet = groundSprites, CurrentFrame = 1 } });
            background.Add(new SpriteComponent { SpriteProvider = new SpriteFromSheet { Sheet = groundSprites, CurrentFrame = 0 } });

            Scene.Entities.Add(ball);
            Scene.Entities.Add(foreground);
            Scene.Entities.Add(background);

            spriteComponent = ball.Get<SpriteComponent>();
            transformComponent = ball.Get<TransformComponent>();

            var decorationScalings = new Vector3(areaSize.X, areaSize.Y, 1);
            background.Get<TransformComponent>().Scale = decorationScalings;
            foreground.Get<TransformComponent>().Scale = decorationScalings/2;
            background.Get<TransformComponent>().Position = new Vector3(0, 0, -1);
            foreground.Get<TransformComponent>().Position = new Vector3(0, 0, 1);

            SpriteAnimation.Play(spriteComponent, 0, spriteComponent.SpriteProvider.SpritesCount-1, AnimationRepeatMode.LoopInfinite, 30);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(() => SpriteAnimation.Stop(spriteComponent)).TakeScreenshot();
            FrameGameSystem.Update(() => SetFrameAndUpdateBall(20, 15)).TakeScreenshot();
            FrameGameSystem.Update(() => SetSpriteImage(ballSprite2)).TakeScreenshot();
        }

        private void SetSpriteImage(SpriteSheet sprite)
        {
            // Keep the current frame when changing sprite provider
            var currentFrame = spriteComponent.CurrentFrame;
            spriteComponent.SpriteProvider = new SpriteFromSheet { Sheet = sprite, CurrentFrame = currentFrame };
        }

        protected override void Update(GameTime time)
        {
            base.Update(time);

            if (!ScreenShotAutomationEnabled)
                UpdateBall((float)time.Elapsed.TotalSeconds);

            if (Input.IsKeyPressed(Keys.D1))
                SetSpriteImage(ballSprite1);
            if (Input.IsKeyPressed(Keys.D2))
                SetSpriteImage(ballSprite2);

            if (Input.IsKeyDown(Keys.Space))
            {
                var provider = spriteComponent.SpriteProvider as SpriteFromSheet;
                Assert.NotNull(provider);
                provider.CurrentFrame = 0;
            }
        }

        private void SetFrameAndUpdateBall(int updateTimes, int frame)
        {
            var provider = spriteComponent.SpriteProvider as SpriteFromSheet;
            Assert.NotNull(provider);
            provider.CurrentFrame = frame;

            for (var i = 0; i < updateTimes; i++)
                UpdateBall(0.033f);
        }

        private void UpdateBall(float totalSeconds)
        {
            const float rotationSpeed = (float)Math.PI / 2;

            var deltaRotation = rotationSpeed * totalSeconds;

            transformComponent.RotationEulerXYZ = new Vector3(0,0, transformComponent.RotationEulerXYZ.Z + deltaRotation);

            var sprite = spriteComponent.SpriteProvider.GetSprite();
            var spriteSize = new Vector2(sprite.Region.Width, sprite.Region.Height);

            for (int i = 0; i < 2; i++)
            {
                var nextPosition = transformComponent.Position[i] + totalSeconds * ballSpeed[i];

                var infBound = -areaSize[i] / 2 + sprite.Center[i];
                var supBound =  areaSize[i] / 2 - sprite.Center[i];

                if (nextPosition > supBound || nextPosition<infBound)
                {
                    ballSpeed[i] = -ballSpeed[i];

                    if (nextPosition > supBound)
                        nextPosition = supBound - (nextPosition - supBound);
                    else
                        nextPosition = infBound + (infBound - nextPosition);
                }

                transformComponent.Position[i] = nextPosition;
            }
        }

        [Fact]
        public void RunTestGame()
        {
            RunGameTest(new SpriteTestGame());
        }
    }
}
