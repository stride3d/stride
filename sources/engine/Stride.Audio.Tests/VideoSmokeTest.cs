// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Video;
using System;

namespace Stride.Audio.Tests
{
    /// <summary>
    /// End-to-end test of the video pipeline: asset compilation (ffmpeg CLI subprocess)
    /// and runtime decode (FFmpeg.AutoGen 7.x + our libav* dylibs/dlls/sos).
    /// </summary>
    public class VideoSmokeTest : GameTestBase
    {
        // Test video: 320x240 H.264 @ 10 fps, 2 s duration. Synthetic testsrc2 pattern.
        private const string TestVideoUrl = "TestVideo";
        private const int VideoWidth = 320;
        private const int VideoHeight = 240;

        private Texture videoTarget;
        private Entity videoEntity;

        public VideoSmokeTest()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];

            // Force FFmpeg backend for deterministic test results: MediaEngine (default on
            // Windows D3D11) clocks on wall-time and decodes asynchronously, so it can't reliably
            // produce a post-action frame within a single game tick.
            VideoBackendRegistry.PreferredBackendName = "FFmpeg";

            // Fixed-step game time so the captured video frame is deterministic across
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60); // 60Hz
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Run after SceneSystem so SaveImage captures the same frame the decoder just wrote.
            FrameGameSystem.DrawOrder = 1000;

            var video = Content.Load<Video.Video>(TestVideoUrl);
            Assert.NotNull(video);

            videoTarget = Texture.New2D(GraphicsDevice, VideoWidth, VideoHeight,
                PixelFormat.R8G8B8A8_UNorm_SRgb,
                TextureFlags.ShaderResource | TextureFlags.RenderTarget);

            videoEntity = new Entity("VideoTest")
            {
                new VideoComponent { Source = video, Target = videoTarget }
            };
            SceneSystem.SceneInstance.RootScene.Entities.Add(videoEntity);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // FFmpeg's D3D11VA hwaccel and software paths produce different output, so capture
            // each frame with a backend suffix and keep separate gold images for each.
            string DecodeSuffix() => videoEntity.Get<VideoComponent>().Instance!.UsesHardwareDecode ? "HW" : "SW";

            // Seek targets are keyframe-aligned (GOP=5 → keyframes at 0/0.5/1.0/1.5s) for
            // deterministic captures, since AVSEEK_FLAG_BACKWARD lands on the preceding keyframe.
            FrameGameSystem.Update(0, () => videoEntity.Get<VideoComponent>().Instance!.Play());

            FrameGameSystem.Draw(0, () => SaveImage(videoTarget, $"Initial.{DecodeSuffix()}"));

            FrameGameSystem.Draw(1, () => videoEntity.Get<VideoComponent>().Instance!.Seek(TimeSpan.FromSeconds(1.0)));
            FrameGameSystem.Draw(2, () => SaveImage(videoTarget, $"SeekForward.{DecodeSuffix()}"));

            FrameGameSystem.Draw(20, () => SaveImage(videoTarget, $"Playback.{DecodeSuffix()}"));

            FrameGameSystem.Draw(40, () => videoEntity.Get<VideoComponent>().Instance!.Seek(TimeSpan.FromSeconds(0.5)));
            FrameGameSystem.Draw(41, () => SaveImage(videoTarget, $"SeekBackward.{DecodeSuffix()}"));
        }

        [Fact]
        public void RunVideoSmokeTest()
        {
            RunGameTest(new VideoSmokeTest());
        }
    }
}
