// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Video;
using Stride.Video.Backends;

namespace Stride.Audio.Tests
{
    /// <summary>
    /// End-to-end test of the video pipeline (asset compilation + runtime decode) parameterised
    /// across every video backend registered on the current platform, and across the FFmpeg
    /// SW/HW variants where applicable.
    ///
    /// Captures are seek-driven: <see cref="VideoInstance.Seek"/> + a fixed frame budget that
    /// lets even the async backends (MediaCodec ImageReader round-trip) deliver before capture.
    /// A separate Theory exercises Play-based timing on FFmpeg only — MediaCodec/MediaEngine
    /// deliver frames asynchronously and can't satisfy mid-playback tick captures deterministically.
    /// </summary>
    public class VideoSmokeTest
    {
        // Test video: 320x240 H.264 @ 10 fps, 2 s duration. Synthetic testsrc2 pattern.
        private const string TestVideoUrl = "TestVideo";
        private const int VideoWidth = 320;
        private const int VideoHeight = 240;

        // Frames to wait between issuing a Seek and capturing — covers worst-case async
        // delivery (MediaCodec: Surface → ImageReader → listener → next Update tick) and
        // MediaEngine cold-start on slow CI runners where the first decode after Stopped
        // can take significantly longer than steady-state seeks.
        private const int SeekSettleFrames = 30;

        // Gold suffix: FFmpeg has both HW (D3D11VA) and SW paths controllable via
        // FFmpegVideoBackendFactory.ForceSoftwareDecode; other backends don't expose a
        // SW/HW knob from the app side so they use a bare backend-name suffix.
        private static string GoldSuffix(string backend, bool forceSW) =>
            backend == "FFmpeg" ? $"FFmpeg.{(forceSW ? "SW" : "HW")}" : backend;

        /// <summary>Per-platform Theory cells, derived at test-discovery time from whatever
        /// backends actually registered themselves via module initializers.</summary>
        public static IEnumerable<object[]> BackendCombos()
        {
            foreach (var factory in VideoBackendRegistry.Factories)
            {
                if (factory.Name == "FFmpeg")
                {
                    // SW path is always exercisable; HW path only when STRIDE_GRAPHICS_API_DIRECT3D11
                    // is the active API at runtime (D3D11VA is the only hwaccel wired today).
                    yield return [factory.Name, /*forceSW*/ true];
#if STRIDE_GRAPHICS_API_DIRECT3D11
                    yield return [factory.Name, /*forceSW*/ false];
#endif
                }
                else
                {
                    // No app-controllable SW path → single cell.
                    yield return [factory.Name, /*forceSW*/ false];
                }
            }
        }

        [SkippableTheory]
        [MemberData(nameof(BackendCombos))]
        public void RunVideoSmokeTestSeek(string backend, bool forceSoftware)
        {
            Skip.If(VideoBackendRegistry.Factories.Count == 0, "No video backend registered on this platform.");
            VideoSmokeTestSeek.Run(backend, forceSoftware);
        }

        [SkippableTheory]
        [MemberData(nameof(BackendCombos))]
        public void RunVideoSmokeTestPlayback(string backend, bool forceSoftware)
        {
            Skip.If(VideoBackendRegistry.Factories.Count == 0, "No video backend registered on this platform.");
            // Play-based timing only deterministic on synchronous backends. FFmpeg decodes on
            // the game thread inside Update; MediaCodec/MediaEngine decode async on a worker
            // thread and the captured frame at tick N depends on decoder/surface latency.
            Skip.IfNot(backend == "FFmpeg", $"Playback timing test is FFmpeg-only (backend: {backend}).");
            VideoSmokeTestPlayback.Run(backend, forceSoftware);
        }

        private abstract class VideoTestGameBase : GameTestBase
        {
            protected readonly string Backend;
            protected readonly bool ForceSoftware;
            protected Texture VideoTarget;
            protected Entity VideoEntity;

            protected VideoTestGameBase(string backend, bool forceSoftware)
            {
                Backend = backend;
                ForceSoftware = forceSoftware;

                GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];

                VideoBackendRegistry.PreferredBackendName = backend;
#if STRIDE_VIDEO_FFMPEG
                FFmpegVideoBackendFactory.ForceSoftwareDecode = forceSoftware;
#endif

                // Fixed-step game time so captures are deterministic.
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

                VideoTarget = Texture.New2D(GraphicsDevice, VideoWidth, VideoHeight,
                    PixelFormat.R8G8B8A8_UNorm_SRgb,
                    TextureFlags.ShaderResource | TextureFlags.RenderTarget);

                VideoEntity = new Entity("VideoTest")
                {
                    new VideoComponent { Source = video, Target = VideoTarget }
                };
                SceneSystem.SceneInstance.RootScene.Entities.Add(VideoEntity);
            }

            protected VideoInstance Instance => VideoEntity.Get<VideoComponent>().Instance!;
        }

        /// <summary>Seek-driven captures: deterministic on every backend because the seek
        /// sync-wait holds <see cref="MediaSynchronizer.CurrentPresentationTime"/> until the
        /// extractor decodes the seek target, and we then wait <see cref="SeekSettleFrames"/>
        /// ticks for the upload to land in the target texture.</summary>
        private sealed class VideoSmokeTestSeek : VideoTestGameBase
        {
            public VideoSmokeTestSeek(string backend, bool forceSoftware) : base(backend, forceSoftware) { }

            internal static void Run(string backend, bool forceSoftware)
            {
                using var game = new VideoSmokeTestSeek(backend, forceSoftware);
                RunGameTest(game);
            }

            protected override void RegisterTests()
            {
                base.RegisterTests();
                var suffix = GoldSuffix(Backend, ForceSoftware);

                // Backends now decode-on-Seek even from Stopped, so the scheduler clock
                // stays frozen at the seek target throughout the settle window — captures
                // land on exactly the keyframe at the seek time.
                int frame = 0;
                FrameGameSystem.Update(frame, () =>
                {
                    Instance.Seek(TimeSpan.FromSeconds(0.0));
                    // Codec init runs synchronously inside the first Seek; UsesHardwareDecode is
                    // reliable from here. Skip the HW variant on devices where D3D11VA acquisition
                    // failed (e.g. WARP on CI) instead of silently scoring a SW-fallback against a
                    // HW-suffixed gold.
                    Skip.If(Backend == "FFmpeg" && !ForceSoftware && !Instance.UsesHardwareDecode,
                        "FFmpeg D3D11VA hwaccel unavailable on this device, skipping HW variant.");
                });
                frame += SeekSettleFrames;
                FrameGameSystem.Draw(frame, () => SaveImage(VideoTarget, $"Initial.{suffix}"));

                frame += 1;
                FrameGameSystem.Update(frame, () => Instance.Seek(TimeSpan.FromSeconds(1.0)));
                frame += SeekSettleFrames;
                FrameGameSystem.Draw(frame, () => SaveImage(VideoTarget, $"SeekForward.{suffix}"));

                frame += 1;
                FrameGameSystem.Update(frame, () => Instance.Seek(TimeSpan.FromSeconds(0.5)));
                frame += SeekSettleFrames;
                FrameGameSystem.Draw(frame, () => SaveImage(VideoTarget, $"SeekBackward.{suffix}"));
            }
        }

        /// <summary>Play-based capture: only deterministic for FFmpeg because its decode
        /// happens inline on the game thread inside Update.</summary>
        private sealed class VideoSmokeTestPlayback : VideoTestGameBase
        {
            public VideoSmokeTestPlayback(string backend, bool forceSoftware) : base(backend, forceSoftware) { }

            internal static void Run(string backend, bool forceSoftware)
            {
                using var game = new VideoSmokeTestPlayback(backend, forceSoftware);
                RunGameTest(game);
            }

            protected override void RegisterTests()
            {
                base.RegisterTests();
                var suffix = GoldSuffix(Backend, ForceSoftware);

                FrameGameSystem.Update(0, () => Instance.Play());
                FrameGameSystem.Draw(20, () =>
                {
                    Skip.If(!ForceSoftware && !Instance.UsesHardwareDecode,
                        $"{Backend} HW decode unavailable on this device (D3D11VA acquisition failed), skipping HW variant.");
                    SaveImage(VideoTarget, $"Playback.{suffix}");
                });
            }
        }
    }
}
