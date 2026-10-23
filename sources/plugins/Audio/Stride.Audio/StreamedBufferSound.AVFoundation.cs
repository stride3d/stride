// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_AVFOUNDATION
using System;
using System.IO;
using AVFoundation;
using AudioToolbox;
using CoreMedia;
using Foundation;
using Stride.Media;

namespace Stride.Audio;

public partial class StreamedBufferSound : SoundBase, IMediaExtractor
{
    partial void InitializeImpl()
    {
        if (mediaDataUrl == null)
            throw new ArgumentNullException(nameof(mediaDataUrl));

        var tempPath = AVFoundationAssetSliceHelper.ExtractAssetSliceToTempFile(mediaDataUrl, startPosition, length, "audio-probe");
        try
        {
            using var url = NSUrl.FromFilename(tempPath);
            using var asset = AVAsset.FromUrl(url);

            var audioTracks = asset.TracksWithMediaType(AVMediaTypes.Audio.GetConstant());
            if (audioTracks == null || audioTracks.Length == 0)
                return;

            var audioTrack = audioTracks[0];
            MediaDuration = TimeSpan.FromSeconds(asset.Duration.Seconds);

            var formatDescriptions = audioTrack.FormatDescriptions;
            if (formatDescriptions == null || formatDescriptions.Length == 0)
                return;

            if (formatDescriptions[0] is CMAudioFormatDescription formatDesc
                && formatDesc.AudioStreamBasicDescription is { } asbd)
            {
                Channels = (int)asbd.ChannelsPerFrame;
                SampleRate = (int)asbd.SampleRate;
            }
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* best-effort */ }
        }
    }
}
#endif
