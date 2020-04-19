// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Streaming;
using Stride.Core.Diagnostics;
using Stride.Media;
using Stride.Audio;
using Stride.Core.IO;
using Stride.Games;

namespace Stride.Video
{
    public sealed partial class VideoInstance : GraphicsResourceBase
    {
        private readonly IServiceRegistry services;
        private readonly VideoSystem videoSystem;
        private readonly ContentManager contentManager;

        private readonly VideoComponent videoComponent;
        private VideoTexture videoTexture;

        private StreamingManager streamingManager;
        private Video currentVideo;

        private float volume = 1f;
        private float speedFactor = 1f;

        private bool mediaInitialized;

        private bool isLooping;
        private PlayRange loopRange;
        private PlayRange playRange;

        //public static readonly Logger Logger = GlobalLogger.GetLogger(nameof(VideoInstance));

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInstance"/> class.
        /// </summary>
        /// <param name="services">The service provider.</param>
        /// <param name="videoComponent">The video component associated with this instance</param>
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

            //TODO: get those fields from the videoAsset or videoComponent
            //SetLoopRange(true, new TimeSpan(0, 0, 20), new TimeSpan(0, 0, 40));
            //SetPlayRange(new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 50));
        }

        public static Logger Logger = GlobalLogger.GetLogger(nameof(VideoInstance));

        public TimeSpan CurrentTime { get; private set; }

        public uint MaxMipMapCount { get; private set; }

        /// <summary>
        /// The duration of the video.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// The current state of the video.
        /// </summary>
        public PlayState PlayState { get; private set; } = PlayState.Stopped;

        /// <summary>
        /// Applies a speed factor the to the video playback. The default value is <c>1.0f</c>.
        /// </summary>
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

        /// <summary>
        /// Define if the video loop or not after reaching the end of the range
        /// </summary>
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

        /// <summary>
        /// if Loop is set to true: set the time at which we restart the video when we arrive at LoopRangeEnd 
        /// </summary>
        public PlayRange LoopRange
        {
            get { return loopRange; }
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
                loopRange.End = end; // do not modify range end.
                loopRangeAdjusted = true;
            }
            if (loopRange.Start > playRange.End)
            {
                Logger.Warning($"Loop start after the play end detected. The loop start has been adjusted to the play end time.");
                var end = loopRange.End;
                loopRange.Start = playRange.End;
                loopRange.End = end; // do not modify range end.
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
            get { return playRange; }
            set
            {
                if (playRange == value)
                    return;

                if (value.Start < TimeSpan.Zero)
                {
                    Logger.Warning($"Invalid negative start time detected '{value.Start}'. The value has been clamped to 0");
                    var end = value.End;
                    value.Start = TimeSpan.Zero;
                    value.End = end; // do not modify range end.
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
        
        /// <summary>
        /// The global volume at which the sound is played.
        /// </summary>
        /// <remarks>Volume is ranging from 0.0f (silence) to 1.0f (full volume). Values beyond those limits are clamped.</remarks>
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
                UpdateAudioVolumeImpl(volume);
        }

        partial void UpdateAudioVolumeImpl(float volume);

        /// <summary>
        /// Release the VideoInstance
        /// </summary>
        public void Release()
        {
            ReleaseMedia();
        }

        private void ReleaseMedia()
        {
            if (IsMediaValid())
            {
                Stop();
                ReleaseMediaImpl();
                DeallocateVideoTexture();
            }
            mediaInitialized = false;
        }

        partial void ReleaseMediaImpl();

        private void UpdateLoopingSettings()
        {
            CheckAndUpdateDataSource();

            if (IsMediaValid())
                UpdateLoopRangeImpl();
        }

        partial void UpdateLoopRangeImpl();

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
                UpdatePlayRangeImpl();
        }
        partial void UpdatePlayRangeImpl();

        private void UpdateSpeedSettings()
        {
            CheckAndUpdateDataSource();

            if (IsMediaValid())
                ChangePlaySpeedImpl();
        }
        partial void ChangePlaySpeedImpl();

        /// <summary>
        /// Plays or resumes the video.
        /// </summary>
        /// <remarks>
        /// If the video was stopped, plays from the beginning. If the video was paused, resumes playing.
        /// If the video is already playing, this method does nothing.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Play()
        {
            CheckAndUpdateDataSource();

            if (PlayState == PlayState.Playing)
                return;
            
            if (IsMediaValid())
            {
                EnsureMedia();
                PlayImpl();
                PlayState = PlayState.Playing;
            }
        }

        /// <summary>
        /// Pauses the video.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pause()
        {
            CheckAndUpdateDataSource();

            if (PlayState == PlayState.Paused)
                return;
            
            if (IsMediaValid())
            {
                EnsureMedia();
                PauseImpl();
                PlayState = PlayState.Paused;
            }
        }

        /// <summary>
        /// Seeks the video to the provided <paramref name="time"/>.
        /// </summary>
        /// <param name="time"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(TimeSpan time)
        {
            CheckAndUpdateDataSource();

            if (IsMediaValid())
            {
                var adjustedTime = TimeSpanExtensions.Clamp(time, playRange.Start, playRange.End);
                SeekImpl(adjustedTime);
                CurrentTime = adjustedTime;
                if (adjustedTime > playRange.Start && PlayState == PlayState.Stopped) // Stop -> currentTime == playRange.Start
                    Pause();
            }
        }

        /// <summary>
        /// Restarts the video from the beginning.
        /// </summary>
        public void RestartVideo()
        {
            Seek(playRange.Start);
        }

        /// <summary>
        /// Stops the video.
        /// </summary>
        /// <remarks>
        /// The resources used by the video are also released.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            CheckAndUpdateDataSource();

            if (PlayState == PlayState.Stopped)
                return;
            
            if (IsMediaValid())
            {
                EnsureMedia();

                StopImpl();

                //Swap back the default texture
                videoTexture?.SetTargetContentToOriginalPlaceholder();

                CurrentTime = playRange.Start;
                PlayState = PlayState.Stopped;
            }
        }

        /// <summary>
        /// Advances the play time to the provided <paramref name="elapsed"/> time.
        /// </summary>
        /// <param name="elapsed"></param>
        /// <returns><c>true</c> if </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(TimeSpan elapsed)
        {
            CheckAndUpdateDataSource();

            if (videoComponent.Target != null)
                streamingManager?.SetResourceStreamingOptions(videoComponent.Target, StreamingOptions.DoNotStream); //TODO revert options after Stop

            videoTexture?.UpdateTargetTexture(videoComponent.Target);
                
            if (IsMediaValid())
                UpdateImpl(ref elapsed);
        }

        private void CheckAndUpdateDataSource()
        {
            if (currentVideo != videoComponent.Source)
            {
                currentVideo = videoComponent.Source;
                InitializeFromDataSource(); // set the new data source
            }
        }

        partial void ReleaseImpl();

        /// <summary>
        /// Implementation of <see cref="Play"/>.
        /// </summary>
        partial void PlayImpl();

        /// <summary>
        /// Implementation of <see cref="Pause"/>.
        /// </summary>
        partial void PauseImpl();

        /// <summary>
        /// Implementation of <see cref="Seek(TimeSpan)"/>.
        /// </summary>
        partial void SeekImpl(TimeSpan time);

        /// <summary>
        /// Implementation of <see cref="Stop"/>.
        /// </summary>
        partial void StopImpl();

        /// <summary>
        /// Implementation of <see cref="Update(TimeSpan)"/>.
        /// </summary>
        partial void UpdateImpl(ref TimeSpan elapsed);

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
            // release current media
            ReleaseMedia();

            // Update the video url information
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

                // Initialize media
                InitializeMedia(url, startPosition, end - startPosition);

                // Set playback properties
                UpdateAudioVolume();
                UpdateLoopingSettings();
                UpdatePlayRangeSettings();
                UpdateSpeedSettings();
            }

            // Do not play and pause the new video. The new video always starts in the 'Stopped' state
            // It is responsibility of the user the revert play state after changing the source video.
        }

        private bool IsMediaValid()
        {
            return mediaInitialized;
        }

        partial void EnsureMedia();

        private void InitializeMedia(string url, long startPosition, long length)
        {
            if (url == null || startPosition < 0 || length < 0)
                return;
            
            InitializeMediaImpl(url, startPosition, length, ref mediaInitialized);
        }

        partial void InitializeMediaImpl(string url, long startPosition, long length, ref bool succeeded);

        private void AllocateVideoTexture(int width, int height)
        {
            // Allocate the video texture that we will copy the video into:
            if (videoTexture != null)
            {
                throw new InvalidOperationException("\"videoTexture\" was not deallocated properly before trying to create a new one!");
            }

            videoTexture = new VideoTexture(GraphicsDevice, services, width, height, videoComponent.MaxMipMapCount);
        }

        private void DeallocateVideoTexture()
        {
            videoTexture?.Dispose();
            videoTexture = null;
        }
    }
}
