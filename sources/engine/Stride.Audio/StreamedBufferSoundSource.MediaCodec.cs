// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC

using System;
using Android.Media;

namespace Stride.Audio
{
    public partial class StreamedBufferSoundSource : DynamicSoundSource
    {
        private MediaExtractor audioMediaExtractor = null;
        private MediaCodec audioMediaDecoder = null;
        private int trackIndexAudio = -1;

        private Java.IO.File InputFile;
        private Java.IO.FileInputStream InputFileStream;

        private bool extractionOutputDone = false;
        private bool extractionInputDone = false;

        partial void InitializeMediaExtractor(string mediaDataUrl, long startPosition, long length)
        {
            if (mediaDataUrl == null)
                throw new ArgumentNullException(nameof(mediaDataUrl));

            ReleaseMediaInternal();

            InputFile = new Java.IO.File(mediaDataUrl);
            if (!InputFile.CanRead())
                throw new Exception(string.Format("Unable to read: {0} ", InputFile.AbsolutePath));

            InputFileStream = new Java.IO.FileInputStream(InputFile.AbsolutePath);

            audioMediaExtractor = new MediaExtractor();
            audioMediaExtractor.SetDataSource(InputFileStream.FD, startPosition, length);
            trackIndexAudio = FindAudioTrack(audioMediaExtractor);
            if (trackIndexAudio < 0)
            {
                ReleaseMediaInternal();
                Logger.Error($"The input file '{mediaDataUrl}' does not contain any audio track.");
                return;
            }

            audioMediaExtractor.SelectTrack(trackIndexAudio);
            var audioFormat = audioMediaExtractor.GetTrackFormat(trackIndexAudio);

            var mime = audioFormat.GetString(MediaFormat.KeyMime);
            audioMediaDecoder = MediaCodec.CreateDecoderByType(mime);
            audioMediaDecoder.Configure(audioFormat, null, null, 0);

            //Get the audio settings
            //should we override the settings (channels, sampleRate, ...) from DynamicSoundSource?
            Channels= audioFormat.GetInteger(MediaFormat.KeyChannelCount);
            SampleRate = audioFormat.GetInteger(MediaFormat.KeySampleRate);
            MediaDuration = TimeSpanExtensions.FromMicroSeconds(audioFormat.GetLong(MediaFormat.KeyDuration));

            audioMediaDecoder.Start();

            extractionOutputDone = false;
            extractionInputDone = false;
        }

        partial void ReleaseMediaInternal()
        {
            audioMediaDecoder?.Stop();
            audioMediaDecoder?.Release();
            audioMediaDecoder = null;

            audioMediaExtractor?.Release();
            audioMediaExtractor = null;

            InputFileStream?.Dispose();
            InputFileStream = null;

            InputFile?.Dispose();
            InputFile = null;
        }

        partial void SeekInternalImpl(TimeSpan seekTime)
        {
            audioMediaDecoder.Flush();
            audioMediaExtractor.SeekTo(seekTime.TotalMicroSeconds(), MediaExtractorSeekTo.ClosestSync);
            extractionOutputDone = false;
            extractionInputDone = false;
        }

        private bool ExtractSomeAudioData(out bool endOfFile)
        {
            endOfFile = extractionOutputDone;
            if (endOfFile)
                return false;

            var hasExtractedData = false;

            int TimeoutUs = 20000;
            MediaCodec.BufferInfo info = new MediaCodec.BufferInfo();

            if (!extractionInputDone)
            {
                int inputBufIndex = audioMediaDecoder.DequeueInputBuffer(TimeoutUs);
                if (inputBufIndex >= 0)
                {
                    Java.Nio.ByteBuffer inputBuffer = audioMediaDecoder.GetInputBuffer(inputBufIndex);

                    //Read the sample data into the ByteBuffer.  This neither respects nor updates inputBuf's position, limit, etc.
                    int chunkSize = audioMediaExtractor.ReadSampleData(inputBuffer, 0);
                    if (chunkSize < 0)
                    {
                        //End of stream: send empty frame with EOS flag set
                        audioMediaDecoder.QueueInputBuffer(inputBufIndex, 0, 0, 0L, MediaCodecBufferFlags.EndOfStream);
                        extractionInputDone = true;
                        //Logger.Verbose("sent input EOS");
                    }
                    else
                    {
                        if (audioMediaExtractor.SampleTrackIndex != trackIndexAudio)
                            Logger.Warning(string.Format("got audio sample from track {0}, expected {1}", audioMediaExtractor.SampleTrackIndex, trackIndexAudio));

                        var presentationTimeMicroSeconds = audioMediaExtractor.SampleTime;
                        audioMediaDecoder.QueueInputBuffer(inputBufIndex, 0, chunkSize, presentationTimeMicroSeconds, 0);

                        audioMediaExtractor.Advance();
                    }
                }
                else
                {
                    //do nothing: the input buffer queue is full (we need to output them first)
                    //continue;
                }
            }

            int decoderStatus = audioMediaDecoder.DequeueOutputBuffer(info, TimeoutUs);

            switch (decoderStatus)
            {
                case (int)MediaCodecInfoState.TryAgainLater:
                    {
                        Logger.Verbose("no output from decoder available");
                        break;
                    }

                case (int)MediaCodecInfoState.OutputFormatChanged:
                    {
                        MediaFormat newFormat = audioMediaDecoder.OutputFormat;
                        string newFormatStr = newFormat.ToString();
                        Logger.Verbose("audio decoder output format changed: " + newFormatStr);
                        break;
                    }

                case (int)MediaCodecInfoState.OutputBuffersChanged:
                    {
                        //deprecated: we just ignore it
                        break;
                    }

                default:
                    {
                        if (decoderStatus < 0)
                            throw new InvalidOperationException(string.Format("unexpected result from audio decoder.DequeueOutputBuffer: {0}", decoderStatus));

                        if ((info.Flags & MediaCodecBufferFlags.EndOfStream) != 0)
                        {
                            Logger.Verbose("audio: output EOS");
                            extractionOutputDone = true;
                        }

                        if (info.Size > 0)
                        {
                            hasExtractedData = true;
                            var buffer = audioMediaDecoder.GetOutputBuffer(decoderStatus);
                            var presentationTime = TimeSpanExtensions.FromMicroSeconds(info.PresentationTimeUs);

                            if (storageBuffer.CountDataBytes + info.Size <= storageBuffer.Data.Length)
                            {
                                buffer.Get(storageBuffer.Data, storageBuffer.CountDataBytes, info.Size); // Read the buffer all at once
                                buffer.Clear(); // MUST DO!!! OTHERWISE THE NEXT TIME YOU GET THIS SAME BUFFER BAD THINGS WILL HAPPEN
                                buffer.Position(0);

                                if (storageBuffer.CountDataBytes == 0)
                                    storageBuffer.PresentationTime = presentationTime;

                                storageBuffer.CountDataBytes += info.Size;
                            }
                            else
                            {
                                Logger.Error("The storage buffer has reached full capacity. Current data will be dropped");
                            }
                        }

                        audioMediaDecoder.ReleaseOutputBuffer(decoderStatus, false);
                        break;
                    }
            }

            endOfFile = extractionOutputDone;
            return hasExtractedData;
        }

        // Selects the video track, if any.
        internal static int FindAudioTrack(MediaExtractor extractor)
        {
            string prefix = "audio/";

            // Select the first video track we find, ignore the rest.
            int numTracks = extractor.TrackCount;
            for (int i = 0; i < numTracks; i++)
            {
                MediaFormat format = extractor.GetTrackFormat(i);
                String mime = format.GetString(MediaFormat.KeyMime);
                if (mime.StartsWith(prefix))
                {
                    Logger.Verbose(string.Format("Extractor selected track {0} ({1}): {2}", i, mime, format));
                    return i;
                }
            }

            return -1;
        }
    }
}

#endif
