// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC
using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Views;
using Stride.Audio;
using Stride.Core;
using Stride.Graphics;
using Stride.Media;
using Stride.Video.Android;

namespace Stride.Video.Backends;

/// <summary>Surface used by VideoInstance to forward MediaCodec-thread notifications into
/// the active backend without leaking Android-specific calls onto VideoInstance itself.</summary>
internal interface IMediaCodecBackend
{
    void OnReceiveNotificationToUpdateVideoTextureSurface();
    bool IsVideoTextureUpdated();
}

internal sealed class MediaCodecVideoBackend : VideoBackend, IMediaCodecBackend
{
    private volatile bool receivedNotificationToUpdateVideoTextureSurface;
    private MediaSynchronizer mediaSynchronizer;
    private MediaCodecVideoExtractor mediaCodecVideoExtractor;
    private Texture textureExternal;
    private Surface videoSurface;
    private SurfaceTexture videoSurfaceTexture;

    private StreamedBufferSound audioSound;
    private SoundInstanceStreamedBuffer audioSoundInstance;
    private readonly List<AudioEmitterSoundController> audioControllers = new();

    private volatile bool isInitialized;

    public MediaCodecVideoBackend(VideoInstance instance) : base(instance) { }

    public override bool Initialize(string url, long startPosition, long length)
    {
        if (mediaSynchronizer != null || mediaCodecVideoExtractor != null)
            throw new InvalidOperationException("mediaCodec has already been initialized");

        receivedNotificationToUpdateVideoTextureSurface = false;

        // Looks like we need to use VK_ANDROID_external_memory_android_hardware_buffer + AImageReader + ANativeWindow + AHardwareBuffer
        throw new NotImplementedException("MediaCodec is not implemented with Vulkan");

#pragma warning disable CS0162 // unreachable — kept for future Vulkan integration
        videoSurfaceTexture = new SurfaceTexture(0);
        videoSurface = new Surface(videoSurfaceTexture);

        mediaSynchronizer = new MediaSynchronizer();

        mediaCodecVideoExtractor = new MediaCodecVideoExtractor(Instance, mediaSynchronizer, videoSurface);
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

        isInitialized = true;
        return true;
#pragma warning restore CS0162
    }

    public override void ReleaseMedia()
    {
        isInitialized = false;
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

        textureExternal?.ReleaseData();
        textureExternal = null;
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
        receivedNotificationToUpdateVideoTextureSurface = false;
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

        // Drop a freshly-decoded frame into the target whenever one is available, even
        // when not Playing — Stopped/Paused freeze scheduler time but a Seek (which forces
        // the worker thread to re-decode) still gets to deliver its frame.
        if (!receivedNotificationToUpdateVideoTextureSurface)
            return;

        videoSurfaceTexture.UpdateTexImage();

        var videoComponent = Instance.VideoComponent;
        if (videoComponent?.Target != null)
        {
            Instance.VideoTexture.SetTargetContentToVideoStream(videoComponent.Target);

            var graphicsContext = Instance.Services.GetSafeServiceAs<GraphicsContext>();
            Instance.VideoTexture.CopyDecoderOutputToTopLevelMipmap(graphicsContext, textureExternal);
            Instance.VideoTexture.GenerateMipMaps(graphicsContext);
        }

        if (mediaSynchronizer.State == PlayState.Playing)
            receivedNotificationToUpdateVideoTextureSurface = false;
    }

    void IMediaCodecBackend.OnReceiveNotificationToUpdateVideoTextureSurface()
        => receivedNotificationToUpdateVideoTextureSurface = true;

    bool IMediaCodecBackend.IsVideoTextureUpdated()
        => !isInitialized || !receivedNotificationToUpdateVideoTextureSurface;
}
#endif
