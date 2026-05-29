// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_AVFOUNDATION
using System;
using System.IO;
using System.Runtime.InteropServices;
using AVFoundation;
using AudioToolbox;
using CoreMedia;
using Foundation;
using Stride.Core.Extensions;
using Stride.Media;

namespace Stride.Audio;

public partial class StreamedBufferSoundSource : DynamicSoundSource
{
    private string audioTempFilePath;
    private AVAsset audioAsset;
    private AVAssetReader audioReader;
    private AVAssetReaderAudioMixOutput audioOutput;
    private AVAssetTrack audioTrack;

    private bool audioExtractionDone;

    partial void InitializeMediaExtractor(string mediaDataUrl, long startPosition, long length)
    {
        if (mediaDataUrl == null)
            throw new ArgumentNullException(nameof(mediaDataUrl));

        ReleaseMediaInternal();

        audioTempFilePath = AVFoundationAssetSliceHelper.ExtractAssetSliceToTempFile(
            mediaDataUrl, startPosition, length, "audio-stream");

        using var url = NSUrl.FromFilename(audioTempFilePath);
        audioAsset = AVAsset.FromUrl(url);

        var audioTracks = audioAsset.TracksWithMediaType(AVMediaTypes.Audio.GetConstant());
        if (audioTracks == null || audioTracks.Length == 0)
        {
            Logger.Error($"The input file '{mediaDataUrl}' does not contain any audio track.");
            ReleaseMediaInternal();
            return;
        }
        audioTrack = audioTracks[0];

        int sourceChannels = 2;
        int sourceSampleRate = 44100;
        var formatDescriptions = audioTrack.FormatDescriptions;
        if (formatDescriptions != null && formatDescriptions.Length > 0
            && formatDescriptions[0] is CMAudioFormatDescription formatDesc
            && formatDesc.AudioStreamBasicDescription is { } asbd)
        {
            sourceChannels = (int)asbd.ChannelsPerFrame;
            sourceSampleRate = (int)asbd.SampleRate;
        }
        Channels = sourceChannels;
        SampleRate = sourceSampleRate;
        MediaDuration = TimeSpan.FromSeconds(audioAsset.Duration.Seconds);

        CreateAudioReader(TimeSpan.Zero);
    }

    partial void SeekInternalImpl(TimeSpan seekTime)
    {
        CreateAudioReader(seekTime);
        audioExtractionDone = false;
    }

    partial void ReleaseMediaInternal()
    {
        DisposeReader();

        audioAsset?.Dispose();
        audioAsset = null;
        audioTrack = null;

        if (audioTempFilePath != null && File.Exists(audioTempFilePath))
        {
            try { File.Delete(audioTempFilePath); } catch { /* best-effort */ }
        }
        audioTempFilePath = null;
    }

    private void CreateAudioReader(TimeSpan startTime)
    {
        DisposeReader();
        if (audioAsset == null || audioTrack == null)
            return;

        audioReader = AVAssetReader.FromAsset(audioAsset, out var error);
        if (error != null)
            throw new InvalidOperationException($"AVAssetReader (audio) create failed: {error.LocalizedDescription}");

        if (startTime > TimeSpan.Zero)
        {
            var startCMTime = new CMTime((long)(startTime.TotalMilliseconds), 1000);
            audioReader.TimeRange = new CMTimeRange { Start = startCMTime, Duration = CMTime.PositiveInfinity };
        }

        // 16-bit signed LE interleaved PCM — the format Stride.Audio consumes downstream.
        var settings = new NSMutableDictionary
        {
            [AVAudioSettings.AVFormatIDKey] = NSNumber.FromUInt32((uint)AudioFormatType.LinearPCM),
            [AVAudioSettings.AVLinearPCMBitDepthKey] = NSNumber.FromInt32(16),
            [AVAudioSettings.AVLinearPCMIsBigEndianKey] = NSNumber.FromBoolean(false),
            [AVAudioSettings.AVLinearPCMIsFloatKey] = NSNumber.FromBoolean(false),
            [AVAudioSettings.AVLinearPCMIsNonInterleaved] = NSNumber.FromBoolean(false),
        };

        audioOutput = new AVAssetReaderAudioMixOutput(new[] { audioTrack }, settings);
        audioReader.AddOutput(audioOutput);
        if (!audioReader.StartReading())
        {
            var err = audioReader.Error;
            throw new InvalidOperationException(
                $"AVAssetReader.StartReading (audio) failed: {(err != null ? err.LocalizedDescription : "unknown")}");
        }
    }

    private void DisposeReader()
    {
        audioOutput?.Dispose();
        audioOutput = null;
        audioReader?.Dispose();
        audioReader = null;
    }

    private bool ExtractSomeAudioData(out bool endOfFile)
    {
        endOfFile = audioExtractionDone;
        if (audioExtractionDone || audioOutput == null)
            return false;

        var sampleBuffer = audioOutput.CopyNextSampleBuffer();
        if (sampleBuffer == null)
        {
            audioExtractionDone = true;
            endOfFile = true;
            return false;
        }

        try
        {
            using var blockBuffer = sampleBuffer.GetDataBuffer();
            if (blockBuffer == null)
                return false;

            int totalLen = (int)blockBuffer.DataLength;
            if (totalLen <= 0)
                return false;

            var presentationTime = TimeSpan.FromSeconds(sampleBuffer.PresentationTimeStamp.Seconds);

            int available = storageBuffer.Data.Length - storageBuffer.CountDataBytes;
            if (totalLen > available)
            {
                Logger.Error("The storage buffer has reached full capacity. Current data will be dropped");
                return false;
            }

            unsafe
            {
                fixed (byte* dst = &storageBuffer.Data[storageBuffer.CountDataBytes])
                {
                    var status = blockBuffer.CopyDataBytes(0, (nuint)totalLen, (IntPtr)dst);
                    if (status != CMBlockBufferError.None)
                    {
                        Logger.Error($"CMBlockBuffer.CopyDataBytes failed: {status}");
                        return false;
                    }
                }
            }

            if (storageBuffer.CountDataBytes == 0)
                storageBuffer.PresentationTime = presentationTime;
            storageBuffer.CountDataBytes += totalLen;
            return true;
        }
        finally
        {
            sampleBuffer.Dispose();
        }
    }
}
#endif
