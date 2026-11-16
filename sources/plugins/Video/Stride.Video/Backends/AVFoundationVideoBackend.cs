// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_AVFOUNDATION
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
/// to VideoToolbox under the hood for hardware decode; a worker thread pulls <see cref="CMSampleBuffer"/>s
/// ahead of playback, and the game thread imports the IOSurface of each <see cref="CVPixelBuffer"/> it
/// presents as a Vulkan VkImage via VK_EXT_metal_objects, then shader-blits it to the user's RGBA target.
/// Zero-copy GPU path.
/// </summary>
internal sealed class AVFoundationVideoBackend : VideoBackend
{
    // Stride bundles compiled assets as offset+length slices inside a single archive file. AVAsset
    // wants a file URL pointing to the start of a self-contained container, so we extract the slice
    // to a temp file at Initialize time. AVAssetResourceLoader is the right long-term answer but
    // adds delegate plumbing we don't need to validate the decode pipeline first.
    private string tempFilePath;
    private AVAsset asset;
    private AVAssetTrack videoTrack;
    // Owns the asset and the temp file once Initialize created it.
    private FrameReader frameReader;
    // Last delivered frame, held until the next one replaces it: the blit reading its IOSurface
    // runs after Update, and releasing sooner lets the decoder pool overwrite the surface mid-read.
    private CMSampleBuffer presentedSampleBuffer;
    // Set by a seek, or by Play from Stopped: the next decoded frame is presented as soon as the
    // worker has it, even when not playing, and the video clock waits for it.
    private bool presentNextFrame;

    private int videoWidth;
    private int videoHeight;
    private TimeSpan frameDuration;
    private long adjustedTicksSinceLastFrame;

    // Audio runs on the StreamedBufferSoundSource worker thread; video clock forwards to the synchronizer.
    private MediaSynchronizer audioSynchronizer;
    private StreamedBufferSound audioSound;
    private SoundInstanceStreamedBuffer audioSoundInstance;
    private readonly List<AudioEmitterSoundController> audioControllers = new();

    public AVFoundationVideoBackend(VideoInstance instance) : base(instance) { }

    public override bool UsesHardwareDecode => true; // AVAssetReader uses VideoToolbox

    public override bool Initialize(string url, long startPosition, long length)
    {
        if (frameReader != null)
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

            InitializeAudio(url, startPosition, length);

            frameReader = new FrameReader(asset, videoTrack, tempFilePath, TimeSpan.Zero);
            asset = null;
            videoTrack = null;
            tempFilePath = null;
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

        frameReader?.Dispose();
        frameReader = null;
        presentNextFrame = false;
        presentedSampleBuffer?.Dispose();
        presentedSampleBuffer = null;

        // Only set when Initialize failed before the frame reader took them.
        asset?.Dispose();
        asset = null;
        videoTrack = null;
        DeleteTempFile(tempFilePath);
        tempFilePath = null;
    }

    public override void Play()
    {
        if (Instance.PlayRange.Start > Instance.CurrentTime)
        {
            Instance.SetCurrentTime(Instance.PlayRange.Start);
            Instance.Seek(Instance.PlayRange.Start);
        }
        else if ((Instance.PlayState == PlayState.Stopped || presentedSampleBuffer == null) && frameReader != null)
        {
            // Present the first frame as soon as it is decoded, matching the FFmpeg backend's
            // behavior (otherwise the caller waits frameDuration/60Hz_tick game frames before any
            // frame surfaces). With no frame presented yet, the decoder may still be starting:
            // the video waits for it, not the game.
            presentNextFrame = true;
        }
        audioSynchronizer?.Play();
    }

    public override void Pause()
    {
        audioSynchronizer?.Pause();
    }

    public override void Stop()
    {
        // FFmpeg backend pattern: seeks back to the play range start, so that Play() resumes from there.
        Instance.Seek(Instance.PlayRange.Start);
        audioSynchronizer?.Stop();
    }

    public override void Seek(TimeSpan time)
    {
        frameReader?.Seek(time);
        presentNextFrame = true;
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

        if (frameReader == null)
            return;

        if (presentNextFrame)
        {
            // Until the decoder delivers it, the video clock does not advance.
            if (frameReader.TryTake(out var firstFrame, out var firstReachedEnd))
            {
                presentNextFrame = false;
                adjustedTicksSinceLastFrame = 0;
                PresentFrame(firstFrame);
            }
            else if (firstReachedEnd)
            {
                OnEndOfMedia();
            }
            return;
        }

        // Mirror the FFmpeg accumulator semantics: speedFactor=0 when not playing freezes auto-advance.
        var speedFactor = Instance.SpeedFactor;
        if (Instance.PlayState != PlayState.Playing)
            speedFactor = 0;

        var frameDurationTicks = frameDuration.Ticks;
        adjustedTicksSinceLastFrame += (long)(elapsed.Ticks * speedFactor);
        var frameCount = (int)(adjustedTicksSinceLastFrame / frameDurationTicks);
        if (frameCount == 0)
            return;

        // Skip-ahead heuristic copied from FFmpeg backend: if we're more than 4 frames behind,
        // seek instead of stepping (cheaper than decoding the gap, and tests don't depend on
        // intermediate frames anyway).
        if (frameCount > 4)
        {
            Instance.Seek(Instance.CurrentTime + TimeSpan.FromTicks(frameDurationTicks * frameCount));
            return;
        }

        // Waits when the worker has not decoded the frame yet, which keeps the video in step with
        // game time; during playback the worker is frames ahead, so this rarely waits.
        CMSampleBuffer sampleBuffer = null;
        for (int i = 0; i < frameCount; i++)
        {
            sampleBuffer?.Dispose();
            sampleBuffer = frameReader.Take();
            if (sampleBuffer == null)
                break;
        }

        if (sampleBuffer == null)
        {
            OnEndOfMedia();
            return;
        }

        adjustedTicksSinceLastFrame %= frameDurationTicks;
        PresentFrame(sampleBuffer);
    }

    private void PresentFrame(CMSampleBuffer sampleBuffer)
    {
        Instance.SetCurrentTime(TimeSpan.FromSeconds(sampleBuffer.PresentationTimeStamp.Seconds));

        using (var imageBuffer = sampleBuffer.GetImageBuffer())
        {
            if (imageBuffer is CVPixelBuffer pixelBuffer)
            {
                UploadFrameToTarget(pixelBuffer);
            }
        }

        presentedSampleBuffer?.Dispose();
        presentedSampleBuffer = sampleBuffer;
    }

    private void OnEndOfMedia()
    {
        presentNextFrame = false;

        var failure = frameReader.TakeFailure();
        if (failure != null)
            VideoInstance.Logger.Warning($"AVFoundationVideoBackend: reading failed at {Instance.CurrentTime}: {failure}");

        if (Instance.IsLooping && Instance.LoopRange.IsValid())
        {
            Instance.SetCurrentTime(Instance.LoopRange.Start);
            Instance.Seek(Instance.LoopRange.Start);
        }
        else
        {
            Instance.Stop();
        }
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
        {
            VideoInstance.Logger.Warning("AVFoundationVideoBackend: the decoded frame has no IOSurface, it is not presented.");
            return;
        }

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

    private static void DeleteTempFile(string path)
    {
        if (path == null || !File.Exists(path))
            return;
        try { File.Delete(path); }
        catch { /* best-effort cleanup */ }
    }

    /// <summary>
    /// Decodes frames ahead of playback on a thread of its own, so that starting the decoder and
    /// seeking, which can take seconds, never block the game thread.
    /// </summary>
    /// <remarks>
    /// The worker owns the asset and the temp file, and releases them when it exits after <see cref="Dispose"/>:
    /// the game thread does not wait for a decoder call in progress, which can take seconds on a cold start.
    /// </remarks>
    private sealed class FrameReader : IDisposable
    {
        // Each queued frame holds a decoded picture: a few frames ahead are enough to hide the
        // decode time of a frame.
        private const int MaxQueuedFrames = 3;

        private readonly object sync = new();
        private readonly Queue<CMSampleBuffer> frames = new();
        private readonly AVAsset asset;
        private readonly AVAssetTrack videoTrack;
        private readonly string tempFilePath;
        private TimeSpan? requestedStart;
        // Where the current reader started, and whether a frame was taken since.
        private TimeSpan decodingStart;
        private bool frameTaken;
        private bool reachedEnd;
        private string failure;
        private bool disposed;

        // Only used on the worker thread.
        private AVAssetReader reader;
        private AVAssetReaderTrackOutput output;

        public FrameReader(AVAsset asset, AVAssetTrack videoTrack, string tempFilePath, TimeSpan start)
        {
            this.asset = asset;
            this.videoTrack = videoTrack;
            this.tempFilePath = tempFilePath;
            requestedStart = start;
            new Thread(Run) { Name = "AVFoundation video decode", IsBackground = true }.Start();
        }

        /// <summary>Drops the decoded frames and restarts decoding at <paramref name="time"/>.</summary>
        public void Seek(TimeSpan time)
        {
            lock (sync)
            {
                // A seek to where decoding already starts keeps the frames decoded so far, such as
                // the first seek after Initialize.
                if (requestedStart == time || (requestedStart == null && decodingStart == time && !frameTaken && !reachedEnd))
                    return;

                ClearFrames();
                requestedStart = time;
                reachedEnd = false;
                failure = null;
                Monitor.PulseAll(sync);
            }
        }

        /// <summary>Takes the next decoded frame, when the worker has one.</summary>
        /// <param name="frame">The frame, which the caller disposes.</param>
        /// <param name="ended">True when no frame is left because decoding reached the end of the media or failed.</param>
        public bool TryTake(out CMSampleBuffer frame, out bool ended)
        {
            lock (sync)
            {
                ended = !TakeFrame(out frame) && reachedEnd;
                return frame != null;
            }
        }

        /// <summary>Takes the next decoded frame, waiting for the worker to decode it.</summary>
        /// <returns>The frame, which the caller disposes, or null when decoding reached the end of the media or failed.</returns>
        public CMSampleBuffer Take()
        {
            lock (sync)
            {
                CMSampleBuffer frame;
                while (!TakeFrame(out frame) && !reachedEnd)
                    Monitor.Wait(sync);
                return frame;
            }
        }

        private bool TakeFrame(out CMSampleBuffer frame)
        {
            if (!frames.TryDequeue(out frame))
                return false;
            frameTaken = true;
            Monitor.PulseAll(sync);
            return true;
        }

        /// <summary>Returns why decoding failed, once, or null when it did not.</summary>
        public string TakeFailure()
        {
            lock (sync)
            {
                var result = failure;
                failure = null;
                return result;
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                disposed = true;
                Monitor.PulseAll(sync);
            }
        }

        private void Run()
        {
            try
            {
                while (true)
                {
                    using var pool = new NSAutoreleasePool();

                    TimeSpan? start;
                    lock (sync)
                    {
                        while (!disposed && requestedStart == null && (output == null || reachedEnd || frames.Count >= MaxQueuedFrames))
                            Monitor.Wait(sync);
                        if (disposed)
                            return;
                        start = requestedStart;
                        requestedStart = null;
                        if (start != null)
                        {
                            decodingStart = start.Value;
                            frameTaken = false;
                        }
                    }

                    CMSampleBuffer frame = null;
                    string error = null;
                    try
                    {
                        if (start != null)
                        {
                            StartReading(start.Value);
                            continue;
                        }

                        frame = output.CopyNextSampleBuffer();
                        if (frame == null && reader.Status == AVAssetReaderStatus.Failed)
                            error = reader.Error?.LocalizedDescription ?? "unknown error";
                    }
                    catch (Exception ex)
                    {
                        DisposeReader();
                        error = ex.Message;
                    }

                    lock (sync)
                    {
                        // A seek or Dispose during the decoder call makes its result stale.
                        if (disposed || requestedStart != null)
                        {
                            frame?.Dispose();
                            continue;
                        }

                        if (frame != null)
                        {
                            frames.Enqueue(frame);
                        }
                        else
                        {
                            reachedEnd = true;
                            failure = error;
                        }
                        Monitor.PulseAll(sync);
                    }
                }
            }
            finally
            {
                DisposeReader();
                lock (sync)
                    ClearFrames();
                asset.Dispose();
                DeleteTempFile(tempFilePath);
            }
        }

        private void StartReading(TimeSpan startTime)
        {
            DisposeReader();

            reader = AVAssetReader.FromAsset(asset, out var error);
            if (error != null)
                throw new InvalidOperationException($"AVAssetReader create failed: {error.LocalizedDescription}");

            // AVAssetReader has no in-place seek: each seek creates a new reader with a time range.
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
            output = new AVAssetReaderTrackOutput(videoTrack, settings)
            {
                // The default of true caches output buffers via the track output's internal pool which
                // is what we want — keeps each frame available until we Dispose() it.
                AlwaysCopiesSampleData = false,
            };
            reader.AddOutput(output);
            if (!reader.StartReading())
            {
                var err = reader.Error;
                throw new InvalidOperationException(
                    $"AVAssetReader.StartReading failed: {(err != null ? err.LocalizedDescription : "unknown")}");
            }
        }

        private void DisposeReader()
        {
            output?.Dispose();
            output = null;
            reader?.Dispose();
            reader = null;
        }

        private void ClearFrames()
        {
            while (frames.TryDequeue(out var frame))
                frame.Dispose();
        }
    }
}
#endif
