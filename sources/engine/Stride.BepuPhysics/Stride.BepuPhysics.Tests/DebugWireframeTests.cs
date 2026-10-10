// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.BepuPhysics.Debug;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering.Compositing;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.BepuPhysics.Tests
{
    public class DebugWireframeTests : GameTestBase
    {
        [Fact]
        public static void ColliderLinesShowOverTheirOwnModel()
        {
            // The debug sphere's facets sit inside the curved surface: the model used to hide most of its lines
            var (withLines, withoutLines) = Render(colliderRadius: 0.5f, modelRadius: 0.5f);
            var changed = 0;
            var total = 0;
            ForEachSpherePixel(withLines, (x, y, color) =>
            {
                total++;
                var other = withoutLines[x, y];
                if (Math.Abs(color.R - other.R) + Math.Abs(color.G - other.G) + Math.Abs(color.B - other.B) > 30)
                    changed++;
            });
            Assert.True(changed > total / 40, $"{changed} of {total} pixels of the sphere show a collider line");
        }

        [Fact]
        public static void ColliderInsideItsModelIsColored()
        {
            var (smaller, _) = Render(colliderRadius: 0.4f, modelRadius: 0.5f);
            var (same, _) = Render(colliderRadius: 0.5f, modelRadius: 0.5f);

            var inside = CountInsideColor(smaller);
            var sameColored = CountInsideColor(same);
            Assert.True(inside > 500, $"{inside} pixels of a 0.4 collider in a 0.5 model are colored as inside");
            Assert.True(sameColored == 0, $"{sameColored} pixels of a 0.5 collider in its 0.5 model are colored as inside");
        }

        private static int CountInsideColor(Frame image)
        {
            var count = 0;
            ForEachSpherePixel(image, (_, _, color) =>
            {
                var hsv = ColorHSV.FromColor(color.ToColor4());
                if (hsv.S > 0.3f && hsv.V > 0.35f && hsv.H is >= 50 and <= 70)
                    count++;
            });
            return count;
        }

        // The pixels inside the sphere's outline, which the camera centers
        private static void ForEachSpherePixel(Frame image, Action<int, int, Color> action)
        {
            int cx = image.Width / 2, cy = image.Height / 2, radius = image.Height / 4;
            for (int y = cy - radius; y < cy + radius; y++)
            {
                for (int x = cx - radius; x < cx + radius; x++)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) < radius * radius)
                        action(x, y, image[x, y]);
                }
            }
        }

        /// <summary> A sphere model and a sphere collider at the origin, rendered with and without the collider's wireframe </summary>
        private static (Frame WithLines, Frame WithoutLines) Render(float colliderRadius, float modelRadius)
        {
            Frame? withLines = null, withoutLines = null;
            var game = new GameTest();
            game.GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
            game.GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;
                var compositor = GraphicsCompositorHelper.CreateDefault(false, clearColor: Color.Gray, graphicsProfile: GraphicsProfile.Level_11_0);
                game.SceneSystem.GraphicsCompositor = compositor;

                var debug = new DebugRenderComponent { Visible = true };
                var sphere = new Entity
                {
                    new ModelComponent(new SphereProceduralModel { Radius = modelRadius }.Generate(game.Services)),
                    new StaticComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = colliderRadius } } } },
                    debug,
                };
                // 0.5 m sphere seen from 1.5 m: it spans about half the image height
                var camera = new Entity { new CameraComponent { Slot = compositor.Cameras[0].ToSlotId() } };
                camera.Transform.Position = new Vector3(0, 0, 1.5f);

                var scene = game.SceneSystem.SceneInstance.RootScene;
                scene.Entities.Add(camera);
                scene.Entities.Add(sphere);

                for (int i = 0; i < 10; i++) await game.Script.NextFrame();
                withLines = Capture(game);
                debug.Visible = false;
                for (int i = 0; i < 10; i++) await game.Script.NextFrame();
                withoutLines = Capture(game);
                game.Exit();
            });
            RunGameTest(game);
            return (withLines!, withoutLines!);
        }

        private static Frame Capture(Game game)
        {
            using var image = game.GraphicsDevice.Presenter.BackBuffer.GetDataAsImage(game.GraphicsContext.CommandList);
            var pixels = image.PixelBuffer[0];
            var bgra = pixels.Format is PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb;
            var colors = pixels.GetPixels<Color>();
            if (bgra)
            {
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new Color(colors[i].B, colors[i].G, colors[i].R, colors[i].A);
            }
            return new Frame(pixels.Width, pixels.Height, colors);
        }

        private sealed record Frame(int Width, int Height, Color[] Pixels)
        {
            public Color this[int x, int y] => Pixels[y * Width + x];
        }
    }
}
