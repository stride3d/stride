#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC

using Android.Media;
using System;
using Stride.Media;
using Android.Views;
using Stride.Core;

namespace Stride.Video.Android
{
    public class MediaCodecVideoMetadata
    {
        public readonly int Width;
        public readonly int Height;
        public readonly TimeSpan Duration;

        public MediaCodecVideoMetadata(int width, int height, TimeSpan duration)
        {
            Width = width;
            Height = height;
            Duration = duration;
        }
    }

    public class MediaCodecVideoExtractor : MediaCodecExtractorBase<MediaCodecVideoMetadata>
    {
        public override MediaType MediaType => MediaType.Video;

        public MediaCodecVideoExtractor(VideoInstance videoInstance, MediaSynchronizer scheduler, Surface decoderOutputSurface) 
            : base(videoInstance, scheduler, decoderOutputSurface)
        {
        }

        protected override void ExtractMediaMetadata(MediaFormat format)
        {
            var videoWidth = format.GetInteger(MediaFormat.KeyWidth);
            var videoHeight = format.GetInteger(MediaFormat.KeyHeight);

            Logger.Verbose(string.Format("Video size: ({0}x{1})", videoWidth, videoHeight));

            MediaMetadata = new MediaCodecVideoMetadata(videoWidth, videoHeight, MediaDuration);
        }

        protected override bool ShouldProcessDequeueOutput(ref TimeSpan waitTime)
        {
            var delayThreshold = TimeSpan.FromMilliseconds(30);
            var videoDelay = Scheduler.CurrentPresentationTime - MediaCurrentTime;

            // we always process the output after a seek request
            if (!isSeekRequestCompleted)
            {
                waitTime = TimeSpan.Zero;
                return true;
            }
            else if (videoDelay < TimeSpan.Zero) // the video is ahead of the synchronizer
            {
                waitTime = -videoDelay;
                return false;
            }
            else if (videoDelay > TimeSpan.FromSeconds(2)) // the video is far behind we try to seek bit
            {
                SeekMediaAt(Scheduler.CurrentPresentationTime + TimeSpan.FromMilliseconds(600));
                return true;
            }
            else
            {
                waitTime = TimeSpan.Zero;
                return true;
            }
        }

        protected override void ProcessOutputBuffer(MediaCodec.BufferInfo bufferInfo, int outputIndex)
        {
            MediaDecoder.ReleaseOutputBuffer(outputIndex, true);

            isSeekRequestCompleted = true;

            VideoInstance.OnReceiveNotificationToUpdateVideoTextureSurface();
        }
    }
}

#endif
