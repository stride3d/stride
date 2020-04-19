// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC

using System;
using Android.Media;
using Stride.Media;

namespace Stride.Audio
{
    /// <summary>
    /// Sound streamed buffer
    /// </summary>
    /// <remarks>
    /// The sound comes from an external process (such like a video decoder, ...) streaming the audio data into a buffer
    /// </remarks>
    public partial class StreamedBufferSound : SoundBase, IMediaExtractor
    {
        partial void InitializeImpl()
        {
            using (var inputFile = new Java.IO.File(mediaDataUrl))
            {
                if (!inputFile.CanRead())
                    throw new Exception(string.Format("Unable to read: {0} ", inputFile.AbsolutePath));

                using (var inputFileStream = new Java.IO.FileInputStream(inputFile.AbsolutePath))
                {
                    var audioMediaExtractor = new MediaExtractor();
                    audioMediaExtractor.SetDataSource(inputFileStream.FD, startPosition, length);
                    var trackIndexAudio = StreamedBufferSoundSource.FindAudioTrack(audioMediaExtractor);
                    if (trackIndexAudio < 0)
                        return;

                    audioMediaExtractor.SelectTrack(trackIndexAudio);
                    var audioFormat = audioMediaExtractor.GetTrackFormat(trackIndexAudio);

                    //Get the audio settings
                    //should we override the settings (channels, sampleRate, ...) from DynamicSoundSource?
                    Channels = audioFormat.GetInteger(MediaFormat.KeyChannelCount);
                    SampleRate = audioFormat.GetInteger(MediaFormat.KeySampleRate);
                    MediaDuration = TimeSpanExtensions.FromMicroSeconds(audioFormat.GetLong(MediaFormat.KeyDuration));
                }
            }
        }
    }
}

#endif
