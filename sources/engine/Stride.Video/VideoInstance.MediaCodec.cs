// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC
using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Graphics;
using Stride.Core.Diagnostics;
using Stride.Audio;
using Stride.Media;

using Android.Graphics;
using Android.Views;
using Stride.Video.Android;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Stride.Video
{
    partial class VideoInstance
    {
        //Set to true when the video mediaCodec has extracted a frame which is ready to be displayed
        private volatile bool ReceivedNotificationToUpdateVideoTextureSurface = false;

        //The main scheduler
        MediaSynchronizer MediaSynchronizer = null;

        //============================================================================
        //Video members
        MediaCodecVideoExtractor MediaCodecVideoExtractor = null;

        //The texture and surface where the video will be extracted
        private Texture TextureExternal = null;
        private Surface VideoSurface = null;
        private SurfaceTexture VideoSurfaceTexture = null;

        //============================================================================
        //Audio members
        //SynchronizedMediaCodecExtractor MediaCodecAudio = null;
        //private Thread AudioMediaCodecThread = null;

        private StreamedBufferSound audioSound = null;
        private SoundInstanceStreamedBuffer audioSoundInstance = null;
        private List<AudioEmitterSoundController> audioControllers = new List<AudioEmitterSoundController>();
        //============================================================================

        private volatile bool IsInitialized = false;

        partial void UpdateLoopRangeImpl()
        {
            if (MediaSynchronizer == null)
                return;

            MediaSynchronizer.IsLooping = IsLooping;
            MediaSynchronizer.LoopRange = LoopRange;
        }

        partial void UpdatePlayRangeImpl()
        {
            if (MediaSynchronizer == null)
                return;

            MediaSynchronizer.PlayRange = PlayRange;
        }

        partial void UpdateAudioVolumeImpl(float volume)
        {
            if (audioSoundInstance != null)
                audioSoundInstance.Volume = volume;

            foreach (var controller in audioControllers)
                controller.Volume = volume;
        }

        partial void PlayImpl()
        {
            if (MediaSynchronizer == null || MediaCodecVideoExtractor == null)
                throw new InvalidOperationException("PlayMedia failed: MediaCodecScheduler is null");
            
            MediaSynchronizer.Play();
        }

        partial void PauseImpl()
        {
            if (MediaSynchronizer == null)
                throw new InvalidOperationException("PauseMedia failed: MediaCodecScheduler is null");

            MediaSynchronizer.Pause();
        }

        partial void SeekImpl(TimeSpan time)
        {
            if (MediaSynchronizer == null)
                throw new InvalidOperationException("PauseMedia failed: MediaCodecScheduler is null");

            MediaSynchronizer.Seek(time);
        }

        partial void StopImpl()
        {
            MediaSynchronizer.Stop();
            ReceivedNotificationToUpdateVideoTextureSurface = false;
        }
        partial void ChangePlaySpeedImpl()
        {
            MediaSynchronizer.SpeedFactor = SpeedFactor;
        }

        partial void UpdateImpl(ref TimeSpan elapsed)
        {
            if (MediaSynchronizer == null)
                return;

            MediaSynchronizer.Update(elapsed);

            if (PlayState == PlayState.Stopped)
                return;

            if (!MediaSynchronizer.ReachedEndOfStream)
            {
                CurrentTime = MediaSynchronizer.CurrentPresentationTime;

                //We receive a notification from mediaCodec thread to update the texture image
                //(the mediaCodec thread won't continue extracting the video until we set ReceiveNotificationToUpdateVideoTextureSurface flag to false)
                if (ReceivedNotificationToUpdateVideoTextureSurface)
                {
                    //The decoder extracted a new frame: we update the GlTextureExternalOes image
                    VideoSurfaceTexture.UpdateTexImage();

                    //Then copy the GlTextureExternalOes into all Textures associated with the Video
                    if (videoComponent?.Target != null)
                    {
                        //swap the video texture if it hasn't been done yet (after the first video frame has been extracted and rendered)
                        videoTexture.SetTargetContentToVideoStream(videoComponent.Target);

                        var graphicsContext = services.GetSafeServiceAs<GraphicsContext>();

                        videoTexture.CopyDecoderOutputToTopLevelMipmap(graphicsContext, TextureExternal);
                        videoTexture.GenerateMipMaps(graphicsContext);
                    }

                    //This will notify the video extractor that he can release this frame output and keep decoding the media
                    if (MediaSynchronizer.State == PlayState.Playing)
                        ReceivedNotificationToUpdateVideoTextureSurface = false;
                }
            }
            else
            {
                Stop();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void EnsureMedia()
        {
            if (MediaSynchronizer == null)
                throw new InvalidOperationException("EnsureMedia failed: MediaCodecScheduler is null");
        }

        partial void InitializeMediaImpl(string url, long startPosition, long length, ref bool succeeded)
        {
            if (MediaSynchronizer != null || MediaCodecVideoExtractor != null)
                throw new InvalidOperationException("mediaCodec has already been initialized");

            if (videoComponent == null)
                throw new ArgumentNullException("videoComponent is null");

            ReceivedNotificationToUpdateVideoTextureSurface = false;

            //==============================================================================================
            //Create the Texture and Surface where the codec will directly extract the video
            //The texture is set as external (GlTextureExternalOes): the mediaCodec API will create it and fill it
            //We don't know its size and format (size / format will depend on the media and on the device implementation)
            TextureExternal = Texture.NewExternalOES(GraphicsDevice);   // TODO: Can we just allocate a mip mapped texture for this?
            int textureId = TextureExternal.TextureId;
            VideoSurfaceTexture = new SurfaceTexture(textureId);
            VideoSurface = new Surface(VideoSurfaceTexture);

            //==============================================================================================
            ///Initialize the mediaCodec
            MediaSynchronizer = new MediaSynchronizer();

            //Init the video extractor
            MediaCodecVideoExtractor = new MediaCodecVideoExtractor(this, MediaSynchronizer, VideoSurface);
            MediaCodecVideoExtractor.Initialize(services, url, startPosition, length);
            MediaSynchronizer.RegisterExtractor(MediaCodecVideoExtractor);
            MediaSynchronizer.RegisterPlayer(MediaCodecVideoExtractor);

            Duration = MediaCodecVideoExtractor.MediaDuration;

            if (MediaCodecVideoExtractor.HasAudioTrack && videoComponent.PlayAudio)
            {
                //Init the audio decoder
                AudioEngine audioEngine = services.GetService<IAudioEngineProvider>()?.AudioEngine;
                if (audioEngine == null)
                    throw new Exception("VideoInstance mediaCodec failed to get the AudioEngine");

                var isSpacialized = videoComponent.AudioEmitters.Any(x=> x!= null);
                audioSound = new StreamedBufferSound(audioEngine, MediaSynchronizer, url, startPosition, length, isSpacialized);
                MediaSynchronizer.RegisterExtractor(audioSound);

                if (isSpacialized) // we play the audio through the emitters if any are set
                {
                    if (audioSound.GetCountChannels() == 1)
                    {
                        //Attach the sound to the audioEmitters
                        foreach (var emitter in videoComponent.AudioEmitters)
                        {
                            if (emitter == null)
                                continue;

                            var controller = emitter.AttachSound(audioSound);
                            MediaSynchronizer.RegisterPlayer(controller);
                            audioControllers.Add(controller);
                        }
                    }
                    else
                    {
                        Logger.Error("Stereo sound tracks cannot be played through audio emitters. The sound track need to be a mono audio track");
                        audioSound.Dispose();
                        audioSound = null;
                    }
                }
                else // otherwise we play the audio as an unlocalized sound.
                {
                    audioSoundInstance = (SoundInstanceStreamedBuffer)audioSound.CreateInstance();
                    MediaSynchronizer.RegisterPlayer(audioSoundInstance);
                }
            }

            var videoMetadata = MediaCodecVideoExtractor.MediaMetadata;
            AllocateVideoTexture(videoMetadata.Width, videoMetadata.Height);

            succeeded = IsInitialized = true;
        }

        partial void ReleaseMediaImpl()
        {
            IsInitialized = false;
            MediaSynchronizer = null;
            
            MediaCodecVideoExtractor?.Dispose();
            MediaCodecVideoExtractor = null;

            audioSoundInstance?.Stop();
            audioSoundInstance?.Dispose();
            audioSoundInstance = null;

            if (audioSound != null)
            {
                foreach (var emitter in videoComponent.AudioEmitters)
                    emitter?.DetachSound(audioSound);

                audioSound?.Dispose();
                audioSound = null;
            }

            videoTexture.SetTargetContentToOriginalPlaceholder();

            TextureExternal?.ReleaseData();
            TextureExternal = null;
        }

        internal void OnReceiveNotificationToUpdateVideoTextureSurface()
        {
            ReceivedNotificationToUpdateVideoTextureSurface = true;
        }

        internal bool IsVideoTextureUpdated()
        {
            if (!IsInitialized) return true;  //return true if we're not initialized: we don't want the mediaCodec thread to stall and keep waiting
            return !ReceivedNotificationToUpdateVideoTextureSurface;
        }
    }
}
#endif
