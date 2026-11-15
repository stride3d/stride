// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_AVFOUNDATION
using System;
using System.IO;
using System.Linq;
using AVFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using Stride.Audio;
using Stride.Core;
using Stride.Graphics;
using Stride.Media;

namespace Stride.Video.Backends;

/// <summary>
/// Video backend for iOS and macOS using AVFoundation. AVAssetReader does the demux and dispatches
/// to VideoToolbox under the hood for hardware decode; we pull <see cref="CMSampleBuffer"/>s on
/// demand and import the resulting <see cref="CVPixelBuffer"/>'s IOSurface as a Vulkan VkImage
/// via VK_EXT_metal_objects, then shader-blit to the user's RGBA target. Zero-copy GPU path.
/// </summary>
internal sealed class AVFoundationVideoBackend : VideoBackend
{
    // Stride bundles compiled assets as offset+length slices inside a single archive file. AVAsset
    // wants a file URL pointing to the start of a self-contained container, so we extract the slice
    // to a temp file at Initialize time. AVAssetResourceLoader is the right long-term answer but
    // adds delegate plumbing we don't need to validate the decode pipeline first.
    private string tempFilePath;
    private AVAsset asset;
    private AVAssetReader reader;
    private AVAssetReaderTrackOutput videoOutput;
    // Last delivered frame, held until the next one replaces it: the blit reading its IOSurface
    // runs after Update, and releasing sooner lets the decoder pool overwrite the surface mid-read.
    private CMSampleBuffer presentedSampleBuffer;
    private AVAssetTrack videoTrack;

    private int videoWidth;
    private int videoHeight;
    private TimeSpan frameDuration;
    private long adjustedTicksSinceLastFrame;

    // Audio runs on the StreamedBufferSoundSource worker thread; video clock forwards to the synchronizer.
    private MediaSynchronizer audioSynchronizer;
    private StreamedBufferSound audioSound;
    private SoundInstanceStreamedBuffer audioSoundInstance;
    private readonly System.Collections.Generic.List<AudioEmitterSoundController> audioControllers = new();

    public AVFoundationVideoBackend(VideoInstance instance) : base(instance) { }

    public override bool UsesHardwareDecode => true; // AVAssetReader uses VideoToolbox

    public override bool Initialize(string url, long startPosition, long length)
    {
        if (reader != null)
            throw new InvalidOperationException("AVFoundationVideoBackend already initialized.");

        try
        {
            tempFilePath = ExtractAssetSliceToTempFile(url, startPosition, length);

            using var assetUrl = NSUrl.FromFilename(tempFilePath);
            asset = AVAsset.FromUrl(assetUrl);

            var videoTracks = asset.TracksWithMediaType(AVMediaTypes.Video.GetConstant());
            if (videoTracks == null || videoTracks.Length == 0)
            {
                VideoInstance.Logger.Warning("AVFoundationVideoBackend: media has no video track.");
                ReleaseMedia();
                return false;
            }
            videoTrack = videoTracks[0];

            var size = videoTrack.NaturalSize;
            videoWidth = (int)size.Width;
            videoHeight = (int)size.Height;

            // NominalFrameRate is 0 for tracks where the muxer didn't write it (rare for H.264).
            // Default to 30 fps as a conservative fallback — used only for the per-frame advance
            // accumulator, not for sample-buffer timing (which comes from the CMSampleBuffer PTS).
            var fps = videoTrack.NominalFrameRate > 0 ? videoTrack.NominalFrameRate : 30f;
            frameDuration = TimeSpan.FromSeconds(1.0 / fps);

            Instance.SetDuration(TimeSpan.FromSeconds(asset.Duration.Seconds));
            Instance.AllocateVideoTexture(videoWidth, videoHeight);

            CreateReader(TimeSpan.Zero);

            InitializeAudio(url, startPosition, length);
            return true;
        }
        catch (Exception ex)
        {
            VideoInstance.Logger.Error($"AVFoundationVideoBackend.Initialize failed: {ex}");
            ReleaseMedia();
            return false;
        }
    }

    public override void ReleaseMedia()
    {
        ReleaseAudio();

        DisposeReader();
        presentedSampleBuffer?.Dispose();
        presentedSampleBuffer = null;
        asset?.Dispose();
        asset = null;
        videoTrack = null;

        if (tempFilePath != null && File.Exists(tempFilePath))
        {
            try { File.Delete(tempFilePath); }
            catch { /* best-effort cleanup */ }
        }
        tempFilePath = null;
    }

    public override void Play()
    {
        if (Instance.PlayRange.Start > Instance.CurrentTime)
        {
            Instance.SetCurrentTime(Instance.PlayRange.Start);
            Instance.Seek(Instance.PlayRange.Start);
        }
        else if (Instance.PlayState == PlayState.Stopped && reader != null)
        {
            // Prime the accumulator so the first frame extracts on the very next Update, matching
            // the FFmpeg backend's behavior (otherwise the caller waits frameDuration/60Hz_tick
            // game frames before any frame surfaces).
            adjustedTicksSinceLastFrame = frameDuration.Ticks;
        }
        audioSynchronizer?.Play();
    }

    public override void Pause()
    {
        audioSynchronizer?.Pause();
    }

    public override void Stop()
    {
        // FFmpeg backend pattern: seeks back to the play range start. Keeps the reader alive in
        // case Play() resumes; the next Seek/Play just recreates the reader at the new position.
        Instance.Seek(Instance.PlayRange.Start);
        audioSynchronizer?.Stop();
    }

    public override void Seek(TimeSpan time)
    {
        // AVAssetReader has no in-place seek — recreate it with a new timeRange. Cheap enough
        // for the smoke test's seek-per-capture pattern (~5-50 ms per seek).
        CreateReader(time);
        adjustedTicksSinceLastFrame = frameDuration.Ticks; // make the first frame extract immediately
        audioSynchronizer?.Seek(time);
    }

    public override void SetAudioVolume(float volume)
    {
        if (audioSoundInstance != null)
            audioSoundInstance.Volume = volume;
        foreach (var controller in audioControllers)
            controller.Volume = volume;
    }

    public override void UpdatePlayRange()
    {
        if (audioSynchronizer != null)
            audioSynchronizer.PlayRange = Instance.PlayRange;
    }

    public override void UpdateLoopRange()
    {
        if (audioSynchronizer != null)
        {
            audioSynchronizer.IsLooping = Instance.IsLooping;
            audioSynchronizer.LoopRange = Instance.LoopRange;
        }
    }

    public override void Update(TimeSpan elapsed)
    {
        audioSynchronizer?.Update(elapsed);

        if (reader == null)
            return;

        // Mirror the FFmpeg accumulator semantics: speedFactor=0 when not playing freezes auto-advance,
        // but a Seek() that just primed the accumulator still gets to decode this tick (= delivers
        // a frame for seek-only captures with no Play()).
        var speedFactor = Instance.SpeedFactor;
        if (Instance.PlayState != PlayState.Playing)
            speedFactor = 0;

        var frameDurationTicks = frameDuration.Ticks;
        adjustedTicksSinceLastFrame += (long)(elapsed.Ticks * speedFactor);
        if (adjustedTicksSinceLastFrame < frameDurationTicks)
            return;

        var frameCount = (int)(adjustedTicksSinceLastFrame / frameDurationTicks);
        if (frameCount == 0)
            return;

        // Skip-ahead heuristic copied from FFmpeg backend: if we're more than 4 frames behind,
        // seek instead of stepping (cheaper than decoding the gap, and tests don't depend on
        // intermediate frames anyway).
        if (frameCount > 4)
        {
            Instance.Seek(Instance.CurrentTime + TimeSpan.FromTicks(frameDurationTicks * frameCount));
            frameCount = 1;
        }

        CMSampleBuffer sampleBuffer = null;
        for (int i = 0; i < frameCount; i++)
        {
            sampleBuffer?.Dispose();
            sampleBuffer = videoOutput.CopyNextSampleBuffer();
            if (sampleBuffer == null)
                break; // EOF or error
        }

        if (sampleBuffer == null)
        {
            // End of media. Reader.Status will be Completed on clean EOF, Failed otherwise.
            if (Instance.IsLooping && Instance.LoopRange.IsValid())
            {
                Instance.SetCurrentTime(Instance.LoopRange.Start);
                Instance.Seek(Instance.LoopRange.Start);
            }
            else
            {
                Instance.Stop();
            }
            return;
        }

        var pts = sampleBuffer.PresentationTimeStamp;
        Instance.SetCurrentTime(TimeSpan.FromSeconds(pts.Seconds));

        using (var imageBuffer = sampleBuffer.GetImageBuffer())
        {
            if (imageBuffer is CVPixelBuffer pixelBuffer)
            {
                UploadFrameToTarget(pixelBuffer);
            }
        }
        adjustedTicksSinceLastFrame %= frameDurationTicks;

        presentedSampleBuffer?.Dispose();
        presentedSampleBuffer = sampleBuffer;
    }

    private void CreateReader(TimeSpan startTime)
    {
        DisposeReader();

        reader = AVAssetReader.FromAsset(asset, out var error);
        if (error != null)
            throw new InvalidOperationException($"AVAssetReader create failed: {error.LocalizedDescription}");

        if (startTime > TimeSpan.Zero)
        {
            // Timescale 600 covers all common video framerates exactly (24/25/30/50/60 fps all
            // hit integer tick counts at 600).
            var startCMTime = new CMTime((long)(startTime.TotalSeconds * 600), 600);
            reader.TimeRange = new CMTimeRange { Start = startCMTime, Duration = CMTime.PositiveInfinity };
        }

        // Ask the reader to deliver decoded BGRA pixel buffers backed by IOSurface. BGRA is the
        // VideoToolbox native output format for H.264/HEVC SDR content — picking it avoids an
        // internal NV12→RGB conversion. The IOSurface is imported as a VkImage in UploadFrameToTarget.
        var settings = new NSMutableDictionary
        {
            [CVPixelBuffer.PixelFormatTypeKey] = NSNumber.FromInt32((int)CVPixelFormatType.CV32BGRA),
            [CVPixelBuffer.IOSurfacePropertiesKey] = new NSDictionary(),
        };
        videoOutput = new AVAssetReaderTrackOutput(videoTrack, settings)
        {
            // The default of true caches output buffers via the track output's internal pool which
            // is what we want — keeps each frame available until we Dispose() it.
            AlwaysCopiesSampleData = false,
        };
        reader.AddOutput(videoOutput);
        if (!reader.StartReading())
        {
            var err = reader.Error;
            throw new InvalidOperationException(
                $"AVAssetReader.StartReading failed: {(err != null ? err.LocalizedDescription : "unknown")}");
        }
    }

    private void DisposeReader()
    {
        videoOutput?.Dispose();
        videoOutput = null;
        reader?.Dispose();
        reader = null;
    }

    private void UploadFrameToTarget(CVPixelBuffer pixelBuffer)
    {
        var target = Instance.VideoComponent.Target;
        if (target == null)
            return;

        // Zero-copy: import the IOSurface that backs the CVPixelBuffer as a BGRA8 VkImage, then
        // shader-blit to the RGBA target. MoltenVK wraps the IOSurface storage directly; the only
        // CPU work is allocating the VkImage/VkImageView (~tens of µs).
        using var ioSurface = pixelBuffer.GetIOSurface();
        if (ioSurface == null)
            return;

        var width = (int)pixelBuffer.Width;
        var height = (int)pixelBuffer.Height;
        var graphicsDevice = Instance.GraphicsDevice;
        var graphicsContext = Instance.Services.GetSafeServiceAs<GraphicsContext>();

        // Swap the user's target with VideoTexture's internal RT and allocate the per-mip views.
        // No-op if already done; matches the MediaEngine backend ordering.
        Instance.VideoTexture.SetTargetContentToVideoStream(target);

        Texture imported = null;
        try
        {
            imported = Texture.NewFromIOSurface(graphicsDevice, ioSurface.Handle.Handle, width, height);
            Instance.VideoTexture.CopyTextureToTopLevelMipmap(graphicsContext, imported);
            Instance.VideoTexture.GenerateMipMaps(graphicsContext);
            Instance.NotifyFramePresented();
        }
        finally
        {
            imported?.Dispose();
        }
    }

    private void InitializeAudio(string url, long startPosition, long length)
    {
        var audioTracks = asset.TracksWithMediaType(AVMediaTypes.Audio.GetConstant());
        if (audioTracks == null || audioTracks.Length == 0)
            return;
        var videoComponent = Instance.VideoComponent;
        if (!videoComponent.PlayAudio)
            return;

        var audioEngine = Instance.Services.GetService<IAudioEngineProvider>()?.AudioEngine
            ?? throw new Exception("VideoInstance AVFoundation failed to get the AudioEngine");

        audioSynchronizer = new MediaSynchronizer();
        audioSynchronizer.PlayRange = Instance.PlayRange;
        audioSynchronizer.IsLooping = Instance.IsLooping;
        audioSynchronizer.LoopRange = Instance.LoopRange;

        var isSpatialized = videoComponent.AudioEmitters.Any(x => x != null);
        audioSound = new StreamedBufferSound(audioEngine, audioSynchronizer, url, startPosition, length, isSpatialized);
        audioSynchronizer.RegisterExtractor(audioSound);

        if (isSpatialized)
        {
            if (audioSound.GetCountChannels() == 1)
            {
                foreach (var emitter in videoComponent.AudioEmitters)
                {
                    if (emitter == null)
                        continue;
                    var controller = emitter.AttachSound(audioSound);
                    audioSynchronizer.RegisterPlayer(controller);
                    audioControllers.Add(controller);
                }
            }
            else
            {
                VideoInstance.Logger.Error("Stereo audio tracks cannot be played through audio emitters. The track needs to be mono.");
                audioSound.Dispose();
                audioSound = null;
                audioSynchronizer = null;
                return;
            }
        }
        else
        {
            audioSoundInstance = (SoundInstanceStreamedBuffer)audioSound.CreateInstance();
            audioSynchronizer.RegisterPlayer(audioSoundInstance);
        }
    }

    private void ReleaseAudio()
    {
        audioSoundInstance?.Stop();
        audioSoundInstance?.Dispose();
        audioSoundInstance = null;

        if (audioSound != null)
        {
            foreach (var emitter in Instance.VideoComponent.AudioEmitters)
                emitter?.DetachSound(audioSound);
            audioSound.Dispose();
            audioSound = null;
        }
        audioControllers.Clear();
        audioSynchronizer = null;
    }

    private static string ExtractAssetSliceToTempFile(string sourcePath, long startPosition, long length)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"stride-video-{Guid.NewGuid():N}.mp4");
        using var src = File.OpenRead(sourcePath);
        using var dst = File.Create(temp);

        src.Seek(startPosition, SeekOrigin.Begin);
        var buffer = new byte[64 * 1024];
        long remaining = length;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = src.Read(buffer, 0, toRead);
            if (read <= 0)
                break;
            dst.Write(buffer, 0, read);
            remaining -= read;
        }
        return temp;
    }
}
#endif
