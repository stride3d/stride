// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Android.Graphics;
using Android.Media;
using Android.Runtime;
using AndroidImage = Android.Media.Image;
using Stride.Audio;
using Stride.Core;
using Stride.Graphics;
using Stride.Media;
using Stride.Video.Android;

namespace Stride.Video.Backends;

internal sealed class MediaCodecVideoBackend : VideoBackend
{
    private MediaSynchronizer mediaSynchronizer;
    private MediaCodecVideoExtractor mediaCodecVideoExtractor;
    private ImageReader imageReader;
    private readonly object imageReaderLock = new();
    private IntPtr rgbaScratch;
    private int rgbaScratchSize;
    private int videoWidth, videoHeight;

    private StreamedBufferSound audioSound;
    private SoundInstanceStreamedBuffer audioSoundInstance;
    private readonly List<AudioEmitterSoundController> audioControllers = new();

    public MediaCodecVideoBackend(VideoInstance instance) : base(instance) { }

    public override bool UsesHardwareDecode => true;

    public override bool Initialize(string url, long startPosition, long length)
    {
        if (mediaSynchronizer != null || mediaCodecVideoExtractor != null)
            throw new InvalidOperationException("mediaCodec has already been initialized");

        // Pre-probe dimensions: ImageReader can't be resized, but MediaCodec.Configure already
        // needs its Surface, so we have to know width/height upfront. Cheap to open a second
        // MediaExtractor here and pull the video format.
        (videoWidth, videoHeight) = ProbeVideoDimensions(url, startPosition, length);

        // CPU YUV path. The zero-copy alternative Texture.NewFromAndroidHardwareBuffer
        // (Stride.Graphics) needs immutable VkSamplerYcbcrConversion descriptor bindings
        // that Stride's effect system doesn't yet expose.
        // No OnImageAvailable listener: it doesn't fire reliably under the emulator's gfxstream
        // BufferQueue, so Update() pulls frames with AcquireLatestImage every tick instead.
        // maxImages is deliberately generous: on a cold/janky start the decoder needs several
        // output buffers to dequeue into before the game thread starts draining, otherwise the
        // gfxstream BufferQueue starves it and it produces nothing for tens of seconds.
        imageReader = ImageReader.NewInstance(videoWidth, videoHeight, ImageFormatType.Yuv420888, maxImages: 8);

        rgbaScratchSize = videoWidth * videoHeight * 4;
        rgbaScratch = Marshal.AllocHGlobal(rgbaScratchSize);

        mediaSynchronizer = new MediaSynchronizer();

        mediaCodecVideoExtractor = new MediaCodecVideoExtractor(Instance, mediaSynchronizer, imageReader.Surface);
        mediaCodecVideoExtractor.Initialize(Instance.Services, url, startPosition, length);
        mediaSynchronizer.RegisterExtractor(mediaCodecVideoExtractor);
        mediaSynchronizer.RegisterPlayer(mediaCodecVideoExtractor);

        Instance.SetDuration(mediaCodecVideoExtractor.MediaDuration);

        var videoComponent = Instance.VideoComponent;
        if (mediaCodecVideoExtractor.HasAudioTrack && videoComponent.PlayAudio)
        {
            var audioEngine = Instance.Services.GetService<IAudioEngineProvider>()?.AudioEngine
                ?? throw new Exception("VideoInstance mediaCodec failed to get the AudioEngine");

            var isSpatialized = videoComponent.AudioEmitters.Any(x => x != null);
            audioSound = new StreamedBufferSound(audioEngine, mediaSynchronizer, url, startPosition, length, isSpatialized);
            mediaSynchronizer.RegisterExtractor(audioSound);

            if (isSpatialized)
            {
                if (audioSound.GetCountChannels() == 1)
                {
                    foreach (var emitter in videoComponent.AudioEmitters)
                    {
                        if (emitter == null)
                            continue;

                        var controller = emitter.AttachSound(audioSound);
                        mediaSynchronizer.RegisterPlayer(controller);
                        audioControllers.Add(controller);
                    }
                }
                else
                {
                    VideoInstance.Logger.Error("Stereo sound tracks cannot be played through audio emitters. The sound track needs to be mono.");
                    audioSound.Dispose();
                    audioSound = null;
                }
            }
            else
            {
                audioSoundInstance = (SoundInstanceStreamedBuffer)audioSound.CreateInstance();
                mediaSynchronizer.RegisterPlayer(audioSoundInstance);
            }
        }

        var videoMetadata = mediaCodecVideoExtractor.MediaMetadata;
        Instance.AllocateVideoTexture(videoMetadata.Width, videoMetadata.Height);

        return true;
    }

    public override void ReleaseMedia()
    {
        mediaSynchronizer = null;

        mediaCodecVideoExtractor?.Dispose();
        mediaCodecVideoExtractor = null;

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

        Instance.VideoTexture?.SetTargetContentToOriginalPlaceholder();

        lock (imageReaderLock)
        {
            imageReader?.Close();
            imageReader = null;
        }

        if (rgbaScratch != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(rgbaScratch);
            rgbaScratch = IntPtr.Zero;
            rgbaScratchSize = 0;
        }
    }

    public override void Play()
    {
        if (mediaSynchronizer == null || mediaCodecVideoExtractor == null)
            throw new InvalidOperationException("PlayMedia failed: MediaCodecScheduler is null");
        mediaSynchronizer.Play();
    }

    public override void Pause()
    {
        if (mediaSynchronizer == null)
            throw new InvalidOperationException("PauseMedia failed: MediaCodecScheduler is null");
        mediaSynchronizer.Pause();
    }

    public override void Stop()
    {
        mediaSynchronizer.Stop();
    }

    public override void Seek(TimeSpan time)
    {
        if (mediaSynchronizer == null)
            throw new InvalidOperationException("Seek failed: MediaCodecScheduler is null");
        mediaSynchronizer.Seek(time);
    }

    public override void SetPlaybackSpeed(float speed) => mediaSynchronizer.SpeedFactor = speed;

    public override void SetAudioVolume(float volume)
    {
        if (audioSoundInstance != null)
            audioSoundInstance.Volume = volume;
        foreach (var controller in audioControllers)
            controller.Volume = volume;
    }

    public override void UpdatePlayRange() => mediaSynchronizer.PlayRange = Instance.PlayRange;

    public override void UpdateLoopRange()
    {
        mediaSynchronizer.IsLooping = Instance.IsLooping;
        mediaSynchronizer.LoopRange = Instance.LoopRange;
    }

    public override void Update(TimeSpan elapsed)
    {
        if (mediaSynchronizer == null)
            return;

        mediaSynchronizer.Update(elapsed);

        if (mediaSynchronizer.ReachedEndOfStream)
        {
            Instance.Stop();
            return;
        }

        Instance.SetCurrentTime(mediaSynchronizer.CurrentPresentationTime);

        // Pull model: poll the ImageReader every tick. AcquireLatestImage returns null when no
        // new frame is queued (cheap), so this only uploads genuinely new frames — and it works
        // even when not Playing, so a Seek's re-decoded frame still lands. More robust than the
        // OnImageAvailable push callback, which the emulator's gfxstream BufferQueue often drops.
        UploadLatestFrameToTarget();
    }

    private unsafe void UploadLatestFrameToTarget()
    {
        AndroidImage image;
        lock (imageReaderLock)
        {
            if (imageReader == null)
                return;
            image = imageReader.AcquireLatestImage();
        }
        if (image == null)
            return;

        try
        {
            var target = Instance.VideoComponent?.Target;
            if (target == null)
                return;

            var planes = image.GetPlanes();
            if (planes == null || planes.Length < 3)
                return;

            ConvertYuv420ToRgba(planes[0], planes[1], planes[2], image.Width, image.Height, (byte*)rgbaScratch);

            var graphicsContext = Instance.Services.GetSafeServiceAs<GraphicsContext>();
            var rgbaSpan = new ReadOnlySpan<byte>((void*)rgbaScratch, rgbaScratchSize);
            target.SetData(graphicsContext.CommandList, rgbaSpan, arrayIndex: 0, mipLevel: 0);

            Instance.NotifyFramePresented();
        }
        finally
        {
            image.Close();
        }
    }

    private static unsafe void ConvertYuv420ToRgba(AndroidImage.Plane yPlane, AndroidImage.Plane uPlane, AndroidImage.Plane vPlane, int width, int height, byte* rgba)
    {
        // BT.601 limited-range YUV->RGB, ITU-R BT.601-7 matrix scaled to Q10 fixed-point.
        var yBuf = yPlane.Buffer;
        var uBuf = uPlane.Buffer;
        var vBuf = vPlane.Buffer;
        var yStride = yPlane.RowStride;
        var uStride = uPlane.RowStride;
        var vStride = vPlane.RowStride;
        var uPixelStride = uPlane.PixelStride;
        var vPixelStride = vPlane.PixelStride;

        var yBase = (byte*)JNIEnv.GetDirectBufferAddress(yBuf.Handle);
        var uBase = (byte*)JNIEnv.GetDirectBufferAddress(uBuf.Handle);
        var vBase = (byte*)JNIEnv.GetDirectBufferAddress(vBuf.Handle);

        for (int y = 0; y < height; y++)
        {
            var yRow = yBase + y * yStride;
            var uRow = uBase + (y >> 1) * uStride;
            var vRow = vBase + (y >> 1) * vStride;
            var dst = rgba + y * width * 4;

            for (int x = 0; x < width; x++)
            {
                int yi = yRow[x] - 16;
                int ui = uRow[(x >> 1) * uPixelStride] - 128;
                int vi = vRow[(x >> 1) * vPixelStride] - 128;
                if (yi < 0) yi = 0;

                int y1192 = 1192 * yi;
                int r = (y1192 + 1634 * vi) >> 10;
                int g = (y1192 - 833 * vi - 400 * ui) >> 10;
                int b = (y1192 + 2066 * ui) >> 10;

                dst[0] = (byte)Math.Clamp(r, 0, 255);
                dst[1] = (byte)Math.Clamp(g, 0, 255);
                dst[2] = (byte)Math.Clamp(b, 0, 255);
                dst[3] = 255;
                dst += 4;
            }
        }
    }

    private static (int width, int height) ProbeVideoDimensions(string url, long startPosition, long length)
    {
        using var file = new Java.IO.FileInputStream(url);
        var probe = new MediaExtractor();
        try
        {
            probe.SetDataSource(file.FD, startPosition, length);
            for (int i = 0; i < probe.TrackCount; i++)
            {
                var format = probe.GetTrackFormat(i);
                var mime = format.GetString(MediaFormat.KeyMime);
                if (mime != null && mime.StartsWith("video/"))
                    return (format.GetInteger(MediaFormat.KeyWidth), format.GetInteger(MediaFormat.KeyHeight));
            }
        }
        finally
        {
            probe.Release();
        }
        throw new InvalidOperationException($"No video track found in {url}");
    }
}
#endif
