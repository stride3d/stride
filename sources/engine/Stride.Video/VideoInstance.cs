// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Streaming;
using Stride.Core.Diagnostics;
using Stride.Media;
using Stride.Core.IO;
using Stride.Games;

namespace Stride.Video
{
    public sealed class VideoInstance : GraphicsResourceBase
    {
        private readonly IServiceRegistry services;
        private readonly VideoSystem videoSystem;
        private readonly ContentManager contentManager;

        private readonly VideoComponent videoComponent;
        private VideoTexture videoTexture;
        private VideoBackend backend;

        private StreamingManager streamingManager;
        private Video currentVideo;

        private float volume = 1f;
        private float speedFactor = 1f;

        private bool mediaInitialized;

        private bool isLooping;
        private PlayRange loopRange;
        private PlayRange playRange;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInstance"/> class.
        /// </summary>
        public VideoInstance([NotNull] IServiceRegistry services, [NotNull] VideoComponent videoComponent)
            : base(services.GetService<IGame>()?.GraphicsDevice)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.videoComponent = videoComponent ?? throw new ArgumentNullException(nameof(videoComponent));

            videoSystem = this.services.GetService<VideoSystem>() ?? throw new InvalidOperationException("The video system has not been added to the services");
            streamingManager = services.GetService<StreamingManager>();
            contentManager = services.GetService<ContentManager>();

            currentVideo = videoComponent.Source;
            IsLooping = videoComponent.LoopVideo;
        }

        public static Logger Logger = GlobalLogger.GetLogger(nameof(VideoInstance));

        public TimeSpan CurrentTime { get; private set; }

        public uint MaxMipMapCount { get; private set; }

        /// <summary>The duration of the video.</summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>The current state of the video.</summary>
        public PlayState PlayState { get; private set; } = PlayState.Stopped;

        /// <summary>True when the active backend currently decodes via hardware acceleration
        /// (e.g. D3D11VA). Only meaningful after the codec has been initialized; will read false
        /// before <see cref="Play"/> has produced the first frame.</summary>
        public bool UsesHardwareDecode => backend?.UsesHardwareDecode ?? false;

        /// <summary>Applies a speed factor to the video playback. The default value is <c>1.0f</c>.</summary>
        public float SpeedFactor
        {
            get => speedFactor;
            set
            {
                if (speedFactor == value)
                    return;
                speedFactor = value;
                UpdateSpeedSettings();
            }
        }

        /// <summary>Define if the video loops after reaching the end of the range.</summary>
        public bool IsLooping
        {
            get => isLooping;
            set
            {
                if (isLooping == value)
                    return;
                isLooping = value;
                UpdateLoopingSettings();
            }
        }

        /// <summary>If <see cref="IsLooping"/> is set, the time at which we restart the video when we reach LoopRangeEnd.</summary>
        public PlayRange LoopRange
        {
            get => loopRange;
            set
            {
                if (loopRange == value)
                    return;
                loopRange = value;
                ValidateLoopRange();
                UpdateLoopingSettings();
            }
        }

        private bool ValidateLoopRange()
        {
            var loopRangeAdjusted = false;

            if (loopRange.Start < playRange.Start)
            {
                Logger.Warning($"Loop start prior to play start detected. The loop start has been adjusted to the play start time.");
                var end = loopRange.End;
                loopRange.Start = playRange.Start;
                loopRange.End = end;
                loopRangeAdjusted = true;
            }
            if (loopRange.Start > playRange.End)
            {
                Logger.Warning($"Loop start after the play end detected. The loop start has been adjusted to the play end time.");
                var end = loopRange.End;
                loopRange.Start = playRange.End;
                loopRange.End = end;
                loopRangeAdjusted = true;
            }
            if (loopRange.End > playRange.End)
            {
                Logger.Warning($"Loop end after play end detected. The value has been adjusted to the play end.");
                loopRange.End = playRange.End;
                loopRangeAdjusted = true;
            }
            if (loopRange.End < playRange.Start)
            {
                Logger.Warning($"Loop end prior to play start detected. The value has been adjusted to the play start.");
                loopRange.End = playRange.Start;
                loopRangeAdjusted = true;
            }
            if (loopRange.Length < TimeSpan.Zero)
            {
                Logger.Warning($"Invalid negative loop range duration detected '{loopRange.Length}'. The value has been clamped to 0");
                loopRange.Length = TimeSpan.Zero;
                loopRangeAdjusted = true;
            }

            return loopRangeAdjusted;
        }

        public PlayRange PlayRange
        {
            get => playRange;
            set
            {
                if (playRange == value)
                    return;

                if (value.Start < TimeSpan.Zero)
                {
                    Logger.Warning($"Invalid negative start time detected '{value.Start}'. The value has been clamped to 0");
                    var end = value.End;
                    value.Start = TimeSpan.Zero;
                    value.End = end;
                }
                if (value.Length < TimeSpan.Zero)
                {
                    Logger.Warning($"Invalid negative play range duration detected '{value.Length}'. The value has been clamped to 0");
                    value.Length = TimeSpan.Zero;
                }

                playRange = value;
                UpdatePlayRangeSettings();

                if (ValidateLoopRange())
                    UpdateLoopingSettings();
            }
        }

        /// <summary>The global volume at which the sound is played. Range [0, 1].</summary>
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
                UpdateAudioVolume();
            }
        }

        private void UpdateAudioVolume()
        {
            CheckAndUpdateDataSource();
            if (IsMediaValid())
                backend.SetAudioVolume(volume);
        }

        /// <summary>Release the VideoInstance.</summary>
        public void Release()
        {
            ReleaseMedia();
        }

        internal void ReleaseMedia()
        {
            if (IsMediaValid())
            {
                Stop();
                backend.ReleaseMedia();
                DeallocateVideoTexture();
            }
            backend?.Dispose();
            backend = null;
            mediaInitialized = false;
        }

        private void UpdateLoopingSettings()
        {
            CheckAndUpdateDataSource();
            if (IsMediaValid())
                backend.UpdateLoopRange();
        }

        private void UpdatePlayRangeSettings()
        {
            CheckAndUpdateDataSource();

            if (PlayState == PlayState.Playing || PlayState == PlayState.Paused)
            {
                if (playRange.Start > CurrentTime)
                {
                    CurrentTime = playRange.Start;
                    Seek(playRange.Start);
                }
            }

            if (IsMediaValid())
                backend.UpdatePlayRange();
        }

        private void UpdateSpeedSettings()
        {
            CheckAndUpdateDataSource();
            if (IsMediaValid())
                backend.SetPlaybackSpeed(speedFactor);
        }

        /// <summary>Plays or resumes the video.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Play()
        {
            CheckAndUpdateDataSource();

            if (PlayState == PlayState.Playing)
                return;

            if (IsMediaValid())
            {
                backend.Play();
                PlayState = PlayState.Playing;
            }
        }

        /// <summary>Pauses the video.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pause()
        {
            CheckAndUpdateDataSource();

            if (PlayState == PlayState.Paused)
                return;

            if (IsMediaValid())
            {
                backend.Pause();
                PlayState = PlayState.Paused;
            }
        }

        /// <summary>Seeks the video to the provided <paramref name="time"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(TimeSpan time)
        {
            CheckAndUpdateDataSource();

            if (IsMediaValid())
            {
                // playRange defaults to an invalid (zero-length) range; clamping against it
                // would force every Seek to land at 0. Fall back to the media's full duration.
                var adjustedTime = playRange.IsValid()
                    ? TimeSpanExtensions.Clamp(time, playRange.Start, playRange.End)
                    : TimeSpanExtensions.Clamp(time, TimeSpan.Zero, Duration);
                backend.Seek(adjustedTime);
                CurrentTime = adjustedTime;
                if (adjustedTime > playRange.Start && PlayState == PlayState.Stopped)
                    Pause();
            }
        }

        /// <summary>Restarts the video from the beginning.</summary>
        public void RestartVideo() => Seek(playRange.Start);

        /// <summary>Stops the video.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            CheckAndUpdateDataSource();

            if (PlayState == PlayState.Stopped)
                return;

            if (IsMediaValid())
            {
                backend.Stop();
                videoTexture?.SetTargetContentToOriginalPlaceholder();
                CurrentTime = playRange.Start;
                PlayState = PlayState.Stopped;
            }
        }

        /// <summary>Advances the play time by <paramref name="elapsed"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(TimeSpan elapsed)
        {
            CheckAndUpdateDataSource();

            if (videoComponent.Target != null)
                streamingManager?.SetResourceStreamingOptions(videoComponent.Target, StreamingOptions.DoNotStream); //TODO revert options after Stop

            videoTexture?.UpdateTargetTexture(videoComponent.Target);

            if (IsMediaValid())
                backend.Update(elapsed);
        }

        private void CheckAndUpdateDataSource()
        {
            if (currentVideo != videoComponent.Source)
            {
                currentVideo = videoComponent.Source;
                InitializeFromDataSource();
            }
        }

        /// <inheritdoc />
        protected internal override bool OnPause()
        {
            if (PlayState == PlayState.Playing)
            {
                Pause();
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        protected internal override void OnResume()
        {
            if (IsDisposed)
                return;
            Play();
        }

        public void InitializeFromDataSource()
        {
            ReleaseMedia();

            string url = null;
            long startPosition = 0;
            long end = 0;

            var source = videoComponent.Source;
            if (source != null)
            {
                var dataUrl = source.CompressedDataUrl;
                var fileProvider = source.FileProvider;

                if (!fileProvider.ContentIndexMap.TryGetValue(dataUrl, out ObjectId objectId) ||
                    !fileProvider.ObjectDatabase.TryGetObjectLocation(objectId, out url, out startPosition, out end))
                {
                    throw new InvalidOperationException("Video files needs to be stored on the virtual file system in a non-compressed form.");
                }

                InitializeMedia(url, startPosition, end - startPosition);

                UpdateAudioVolume();
                UpdateLoopingSettings();
                UpdatePlayRangeSettings();
                UpdateSpeedSettings();
            }

            // New video always starts in Stopped state.
        }

        private bool IsMediaValid() => mediaInitialized && backend != null;

        private void InitializeMedia(string url, long startPosition, long length)
        {
            if (url == null || startPosition < 0 || length < 0)
                return;

            var factory = videoSystem.ActiveBackendFactory
                ?? throw new InvalidOperationException("No video backend is registered or supported on this platform.");
            backend = factory.CreateBackend(this);
            mediaInitialized = backend.Initialize(url, startPosition, length);
            if (!mediaInitialized)
            {
                backend.Dispose();
                backend = null;
            }
        }

        internal void AllocateVideoTexture(int width, int height)
        {
            if (videoTexture != null)
                throw new InvalidOperationException("\"videoTexture\" was not deallocated properly before trying to create a new one!");

            videoTexture = new VideoTexture(GraphicsDevice, services, width, height, videoComponent.MaxMipMapCount);
        }

        private void DeallocateVideoTexture()
        {
            videoTexture?.Dispose();
            videoTexture = null;
        }

        // Backend-accessible state. Internal to keep VideoInstance's public surface stable.
        internal IServiceRegistry Services => services;
        internal VideoComponent VideoComponent => videoComponent;
        internal VideoTexture VideoTexture => videoTexture;
        internal void SetCurrentTime(TimeSpan time) => CurrentTime = time;
        internal void SetDuration(TimeSpan duration) => Duration = duration;

        // MediaCodec-thread notification forwarders. Safe no-op on non-MediaCodec backends.
#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC
        internal void OnReceiveNotificationToUpdateVideoTextureSurface()
            => (backend as Backends.IMediaCodecBackend)?.OnReceiveNotificationToUpdateVideoTextureSurface();

        internal bool IsVideoTextureUpdated()
            => (backend as Backends.IMediaCodecBackend)?.IsVideoTextureUpdated() ?? true;
#endif
    }
}
