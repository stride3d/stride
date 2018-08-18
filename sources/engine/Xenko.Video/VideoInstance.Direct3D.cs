// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_DIRECT3D11
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SharpDX.MediaFoundation;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Graphics;
using Xenko.Media;

namespace Xenko.Video
{
    partial class VideoInstance
    {
        private MediaEngine mediaEngine;
        private Texture videoOutputTexture;
        private SharpDX.DXGI.Surface videoOutputSurface;
        private Stream videoFileStream;
        private ByteStream videoDataStream;

        private int videoWidth;
        private int videoHeight;

        private bool reachedEOF;

        private static MediaEngineClassFactory mediaEngineFactory = new MediaEngineClassFactory();

        partial void ReleaseMediaImpl()
        {
            mediaEngine?.Shutdown();
            mediaEngine?.Dispose();
            mediaEngine = null;

            // Randomly crashes in sharpDX and the native code when disabling this
            // The stream is problably accessed after disposal due to communication latency 
            // Unfortunately we don't receive any events after the call to Shutdown where we could dispose those

            //videoDataStream?.Dispose();
            //videoDataStream = null;
            //videoFileStream?.Dispose();
            //videoFileStream = null;
            
            videoOutputSurface?.Dispose();
            videoOutputSurface = null; 
            videoOutputTexture?.Dispose();
            videoOutputTexture = null;
        }

        partial void PlayImpl()
        {
            if (playRange.Start > CurrentTime)
                Seek(playRange.Start);

            mediaEngine.Play();
        }

        partial void PauseImpl()
        {
            mediaEngine.Pause();
        }

        partial void StopImpl()
        {
            mediaEngine.Pause();
            Seek(playRange.Start);
        }

        partial void SeekImpl(TimeSpan time)
        {
            mediaEngine.CurrentTime = time.TotalSeconds;
            reachedEOF = false;
        }

        partial void ChangePlaySpeedImpl()
        {
            mediaEngine.PlaybackRate = SpeedFactor;
        }

        partial void UpdatePlayRangeImpl()
        {
            if (playRange.Start > CurrentTime)
                Seek(playRange.Start);
        }

        partial void UpdateAudioVolumeImpl(float volume)
        {
            mediaEngine.Volume = volume;
        }

        partial void UpdateImpl(ref TimeSpan elapsed)
        {
            if (videoOutputSurface == null || PlayState == PlayState.Stopped)
                return;

            //Transfer frame if a new one is available
            if (mediaEngine.OnVideoStreamTick(out var presentationTimeTicks))
            {
                CurrentTime = TimeSpan.FromTicks(presentationTimeTicks);

                // Check end of media
                var endOfMedia = reachedEOF;
                if (!endOfMedia)
                {
                    //check the video loop and play range
                    if (PlayRange.IsValid() && CurrentTime > PlayRange.End)
                    {
                        endOfMedia = true;
                    }
                    else if (IsLooping && LoopRange.IsValid() && CurrentTime > LoopRange.End)
                    {
                        endOfMedia = true;
                    }
                }

                if (endOfMedia)
                {
                    if (IsLooping)
                    {
                        //Restart the video at LoopRangeStart
                        Seek(LoopRange.Start);
                    }
                    else
                    {
                        //stop the video
                        Stop();
                        return;
                    }
                }

                if (videoComponent.Target != null && videoOutputSurface != null && videoOutputTexture != null)
                {
                    videoTexture.SetTargetContentToVideoStream(videoComponent.Target);

                    // Now update the video texture with data of the new video frame:
                    var graphicsContext = services.GetSafeServiceAs<GraphicsContext>();

                    mediaEngine.TransferVideoFrame(videoOutputSurface, null, new SharpDX.Mathematics.Interop.RawRectangle(0, 0, videoWidth, videoHeight), null);
                    videoTexture.CopyDecoderOutputToTopLevelMipmap(graphicsContext, videoOutputTexture);

                    videoTexture.GenerateMipMaps(graphicsContext);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void EnsureMedia()
        {
            if (mediaEngine == null)
                throw new InvalidOperationException();
        }

        partial void InitializeMediaImpl(string url, long startPosition, long length, ref bool succeeded)
        {
            succeeded = true;

            if (mediaEngine != null)
                throw new InvalidOperationException();

            try
            {
                //Assign our dxgi manager, and set format to bgra
                var attr = new MediaEngineAttributes
                {
                    VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    DxgiManager = videoSystem.DxgiDeviceManager,
                };

                mediaEngine = new MediaEngine(mediaEngineFactory, attr);

                // Register our PlayBackEvent
                mediaEngine.PlaybackEvent += OnPlaybackCallback;

                // set the video source 
                var mediaEngineEx = mediaEngine.QueryInterface<MediaEngineEx>();
                
                var databaseUrl = videoComponent.Source?.CompressedDataUrl;
                if (databaseUrl == null)
                {
                    Logger.Info("The video source is null. The video won't play.");
                    throw new Exception();
                }
                videoFileStream = contentManager.OpenAsStream(databaseUrl, StreamFlags.Seekable);
                videoDataStream = new ByteStream(videoFileStream);

                // Creates an URL to the file
                var uri = new Uri(url, UriKind.RelativeOrAbsolute);

                // Set the source stream
                mediaEngineEx.SetSourceFromByteStream(videoDataStream, uri.AbsoluteUri);
            }
            catch
            {
                succeeded = false;
                ReleaseMedia();
            }
        }

        private void CompleteMediaInitialization()
        {
            //Get our video size
            mediaEngine.GetNativeVideoSize(out videoWidth, out videoHeight);
            Duration = TimeSpan.FromSeconds(mediaEngine.Duration);

            //Get DXGI surface to be used by our media engine
            videoOutputTexture = Texture.New2D(GraphicsDevice, videoWidth, videoHeight, 1, PixelFormat.B8G8R8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            videoOutputSurface = videoOutputTexture.NativeResource.QueryInterface<SharpDX.DXGI.Surface>();

            AllocateVideoTexture(videoWidth, videoHeight);

            if (videoComponent.PlayAudio != true || videoComponent.AudioEmitters.Any(e => e != null))
                mediaEngine.Muted = true;
        }

        /// <summary>
        /// Called when [playback callback].
        /// </summary>
        /// <param name="playEvent">The play event.</param>
        /// <param name="param1">The param1.</param>
        /// <param name="param2">The param2.</param>
        private void OnPlaybackCallback(MediaEngineEvent playEvent, long param1, int param2)
        {
            switch (playEvent)
            {
                case MediaEngineEvent.ResourceLost:
                case MediaEngineEvent.StreamRenderingerror:
                case MediaEngineEvent.Suspend:
                case MediaEngineEvent.Abort:
                case MediaEngineEvent.Emptied:
                case MediaEngineEvent.Stalled:
                    break;
                case MediaEngineEvent.LoadedMetadata:
                    CompleteMediaInitialization();
                    break;
                case MediaEngineEvent.Error:
                    Logger.Error($"Failed to load the video source. The file codec or format is likely not to be supported. MedieEngine error code=[{(MediaEngineErr)param1}], Windows error code=[{param2}]");
                    ReleaseMedia();
                    break;
                case MediaEngineEvent.FirstFrameReady:
                    break;
                case MediaEngineEvent.LoadedData:
                    break;
                case MediaEngineEvent.CanPlay:
                    break;
                case MediaEngineEvent.Seeked:
                    break;
                case MediaEngineEvent.Ended:
                    reachedEOF = true;
                    break;
                case MediaEngineEvent.LoadStart:
                case MediaEngineEvent.Progress:
                case MediaEngineEvent.Waiting:
                case MediaEngineEvent.Playing:
                case MediaEngineEvent.CanPlayThrough:
                case MediaEngineEvent.Seeking:
                case MediaEngineEvent.Play:
                case MediaEngineEvent.Pause:
                case MediaEngineEvent.TimeUpdate:
                case MediaEngineEvent.RateChange:
                case MediaEngineEvent.DurationChange:
                case MediaEngineEvent.VolumeChange:
                case MediaEngineEvent.FormatChange:
                case MediaEngineEvent.PurgeQueuedEvents:
                case MediaEngineEvent.TimelineMarker:
                case MediaEngineEvent.BalanceChange:
                case MediaEngineEvent.DownloadComplete:
                case MediaEngineEvent.BufferingStarted:
                case MediaEngineEvent.BufferingEnded:
                case MediaEngineEvent.FrameStepCompleted:
                case MediaEngineEvent.NotifyStableState:
                case MediaEngineEvent.Trackschange:
                case MediaEngineEvent.OpmInformation:
                case MediaEngineEvent.DelayloadeventChanged:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(playEvent), playEvent, null);
            }
        }
    }
}
#endif
