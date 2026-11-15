#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC

using Android.Media;
using Stride.Media;
using System;

namespace Stride.Video.Android
{
    public class MediaCodecAudioMetadata
    {
        public readonly int ChannelsCount;
        public readonly int SampleRate;
        public readonly TimeSpan Duration;

        public MediaCodecAudioMetadata(int channelsCount, int sampleRate, TimeSpan duration)
        {
            ChannelsCount = channelsCount;
            SampleRate = sampleRate;
            Duration = duration;
        }
    }

    public class MediaCodecAudioExtractor : MediaCodecExtractorBase<MediaCodecAudioMetadata>
    {
        private byte[] audioChunckBuffer = null;

        public MediaCodecAudioExtractor(VideoInstance videoInstance, MediaSynchronizer scheduler) : 
            base(videoInstance, scheduler)
        {
        }

        public override MediaType MediaType => MediaType.Audio;

        protected override void ExtractMediaMetadata(MediaFormat format)
        {
            var audioChannels = format.GetInteger(MediaFormat.KeyChannelCount);
            var audioSampleRate = format.GetInteger(MediaFormat.KeySampleRate);

            MediaMetadata = new MediaCodecAudioMetadata(audioChannels, audioSampleRate, MediaDuration);
        }

        protected override void ProcessOutputBuffer(MediaCodec.BufferInfo bufferInfo, int outputIndex)
        {
            var buffer = MediaDecoder.GetOutputBuffer(outputIndex);

            if (audioChunckBuffer == null || audioChunckBuffer.Length < bufferInfo.Size)
            {
                //Create a reusable byte buffer with some extra marging
                audioChunckBuffer = new byte[bufferInfo.Size + 512];
            }

            buffer.Get(audioChunckBuffer, 0, bufferInfo.Size); // Read the buffer all at once
            buffer.Clear(); // Must do, otherwise next time we receive this outputbuffer it might be bad

            Logger.Debug("Audio chunck" + " time:" + bufferInfo.PresentationTimeUs + " size: " + bufferInfo.Size);

            MediaDecoder.ReleaseOutputBuffer(outputIndex, false);

            isSeekRequestCompleted = true;
        }

        protected override bool ShouldProcessDequeueOutput(ref TimeSpan waitTime)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
