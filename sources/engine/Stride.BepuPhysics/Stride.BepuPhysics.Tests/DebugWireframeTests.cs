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

        [Fact]
        public static void ConcaveModelHidesItsFartherLines()
        {
            // A small box behind a larger one, both parts of one collidable and of its visual model: the near box hides the far one
            var (withLines, withoutLines) = Render(game =>
            {
                var far = new Entity { new ModelComponent(new CubeProceduralModel { Size = new Vector3(0.3f) }.Generate(game.Services)) };
                far.Transform.Position = new Vector3(0, 0, -1.5f);
                var near = new Entity
                {
                    new ModelComponent(new CubeProceduralModel { Size = new Vector3(0.6f) }.Generate(game.Services)),
                    new StaticComponent
                    {
                        Collider = new CompoundCollider
                        {
                            Colliders =
                            {
                                new BoxCollider { Size = new Vector3(0.6f) },
                                new BoxCollider { Size = new Vector3(0.3f), PositionLocal = new Vector3(0, 0, -1.5f) },
                            },
                        },
                    },
                };
                near.AddChild(far);
                return near;
            }, cameraDistance: 3f);
            // The far box's outline falls well inside the near box's front face, away from that face's own edges
            var changed = CountChanged(withLines, withoutLines, withLines.Height / 12);
            Assert.True(changed == 0, $"{changed} pixels show lines of the far box through the near one");
        }

        /// <summary> Pixels within <paramref name="radius"/> of the image center whose color differs between both frames </summary>
        private static int CountChanged(Frame a, Frame b, int radius)
        {
            var count = 0;
            int cx = a.Width / 2, cy = a.Height / 2;
            for (int y = cy - radius; y < cy + radius; y++)
            {
                for (int x = cx - radius; x < cx + radius; x++)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) >= radius * radius)
                        continue;
                    Color p = a[x, y], q = b[x, y];
                    if (Math.Abs(p.R - q.R) + Math.Abs(p.G - q.G) + Math.Abs(p.B - q.B) > 30)
                        count++;
                }
            }
            return count;
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
        // 0.5 m sphere seen from 1.5 m: it spans about half the image height
        private static (Frame WithLines, Frame WithoutLines) Render(float colliderRadius, float modelRadius)
            => Render(game => new Entity
            {
                new ModelComponent(new SphereProceduralModel { Radius = modelRadius }.Generate(game.Services)),
                new StaticComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = colliderRadius } } } },
            }, cameraDistance: 1.5f);

        /// <summary> The collidable <paramref name="create"/> makes, at the origin, rendered with and without its wireframe </summary>
        private static (Frame WithLines, Frame WithoutLines) Render(Func<Game, Entity> create, float cameraDistance)
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
                var collidable = create(game);
                collidable.Add(debug);
                var camera = new Entity { new CameraComponent { Slot = compositor.Cameras[0].ToSlotId() } };
                camera.Transform.Position = new Vector3(0, 0, cameraDistance);

                var scene = game.SceneSystem.SceneInstance.RootScene;
                scene.Entities.Add(camera);
                scene.Entities.Add(collidable);

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
