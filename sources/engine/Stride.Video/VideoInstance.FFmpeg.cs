// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG && !STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Video.FFmpeg;
using Stride.Graphics;
using Stride.Media;

namespace Stride.Video
{
    partial class VideoInstance
    {
        private FFmpegMedia media;
        private VideoStream stream;

        private long adjustedTicksSinceLastFrame = 0L;
        
        partial void ReleaseMediaImpl()
        {
            media.RemoveDisposeBy(this);
            media.Dispose();
            media = null;
        }

        partial void PlayImpl()
        {
            //Start playing at a specific spot?
            if (playRange.Start > CurrentTime)
            {
                CurrentTime = playRange.Start;
                Seek(playRange.Start);
            }
        }

        partial void StopImpl()
        {
            Seek(playRange.Start);
        }

        partial void SeekImpl(TimeSpan time)
        {
            media.SeekToTime(stream, stream.TimeToTimestamp(time));
            adjustedTicksSinceLastFrame = stream.FrameDuration.Ticks; // First frame need to be loaded right away
        }

        partial void UpdateImpl(ref TimeSpan elapsed)
        {
            if (stream == null)
                return;

            if (PlayState == PlayState.Stopped)
                return;

            var speedFactor = SpeedFactor;
            if (PlayState == PlayState.Paused)
                speedFactor = 0;

            // Compare elapsed time with video framerate
            var frameDurationTicks = stream.FrameDuration.Ticks;
            adjustedTicksSinceLastFrame += (long)(elapsed.Ticks * speedFactor);
            if (adjustedTicksSinceLastFrame < frameDurationTicks)
                return;

            var frameCount = (int)(adjustedTicksSinceLastFrame / frameDurationTicks);
            if (frameCount == 0)
                // Note: in case of slow speed factor, we might not need to update at each draw
                return;

            if (frameCount > 4)
            {
                // Reading more than a few frames can be expensive, better seek.
                // FIXME: we might need a heuristic here to auto-adapt. It is probably dependent on the video being played (e.g. resolution, codec, file size, etc.)
                Seek(CurrentTime + TimeSpan.FromTicks(frameDurationTicks * frameCount));
                frameCount = 1;
            }

            // Extract the frames
            var extractedFrameCount = media.ExtractFrames(stream, frameCount);
            if (extractedFrameCount > 0)
                adjustedTicksSinceLastFrame = adjustedTicksSinceLastFrame % stream.FrameDuration.Ticks;

            // Get the last one
            var streamInfo = media.GetStreamInfo(stream);
            if (streamInfo?.Image == null)
                return;

            // Check end of media
            bool endOfMedia = streamInfo.ReachedEnd;
            if (!endOfMedia)
            {
                if (extractedFrameCount > 0)
                {
                    CurrentTime = stream.TimestampToTime(streamInfo.Image.Timestamp);
                }

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
                    //(ToCheck: is there a better way to do this (directly updating CurrentTime does not seem good, but if not doing, it will not work)) 
                    CurrentTime = LoopRange.Start;
                    Seek(LoopRange.Start);
                    return;
                }
                else
                {
                    //stop the video
                    Stop();
                    return;
                }
            }

            // return if the frame extraction failed and didn't reached and of the video
            if (extractedFrameCount == 0)
                return;

            if (videoComponent.Target != null)
            {
                videoTexture.SetTargetContentToVideoStream(videoComponent.Target);

                // Now update the video texture with data of the new video frame:
                var graphicsContext = services.GetSafeServiceAs<GraphicsContext>();

                if (streamInfo.Codec.IsHardwareAccelerated && streamInfo.Image == null)
                    videoTexture.CopyDecoderOutputToTopLevelMipmap(graphicsContext, streamInfo.Codec.DecoderOutputTexture);
                else
                    videoTexture.UpdateTopLevelMipmapFromData(graphicsContext, streamInfo.Image);

                videoTexture.GenerateMipMaps(graphicsContext);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void EnsureMedia()
        {
            if (media == null)
                throw new InvalidOperationException();
        }

        partial void InitializeMediaImpl(string url, long startPosition, long length, ref bool succeeded)
        {
            succeeded = false;

            if (media != null)
                throw new InvalidOperationException();

            try
            {
                // Create and open the media
                media = new FFmpegMedia(GraphicsDevice);
                media.DisposeBy(this);
                media.Open(url, startPosition, length);
                // Get the first video stream
                stream = media.Streams.OfType<VideoStream>().FirstOrDefault();
                if (stream == null)
                {
                    ReleaseMedia();
                    Duration = TimeSpan.Zero;
                    Logger.Warning("This media doesn't contain a video stream.");
                    return;
                }

                Duration = stream.Duration;
                AllocateVideoTexture(stream.Width, stream.Height);
            }
            catch
            {
                ReleaseMedia();
                return;
            }

            succeeded = true;
        }
    }
}
#endif
