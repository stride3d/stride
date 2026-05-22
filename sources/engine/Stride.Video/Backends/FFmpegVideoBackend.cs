// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
using System;
using System.Linq;
using Stride.Core;
using Stride.Graphics;
using Stride.Media;
using Stride.Video.FFmpeg;

namespace Stride.Video.Backends;

internal sealed class FFmpegVideoBackend : VideoBackend
{
    private FFmpegMedia media;
    private VideoStream stream;
    private long adjustedTicksSinceLastFrame;

    public FFmpegVideoBackend(VideoInstance instance) : base(instance) { }

    public override bool UsesHardwareDecode =>
        stream != null && media?.GetStreamInfo(stream)?.Codec?.IsHardwareAccelerated == true;

    public override bool Initialize(string url, long startPosition, long length)
    {
        if (media != null)
            throw new InvalidOperationException();

        try
        {
            media = new FFmpegMedia(Instance.GraphicsDevice);
            media.DisposeBy(Instance);
            media.Open(url, startPosition, length);
            stream = media.Streams.OfType<VideoStream>().FirstOrDefault();
            if (stream == null)
            {
                ReleaseMedia();
                Instance.SetDuration(TimeSpan.Zero);
                VideoInstance.Logger.Warning("This media doesn't contain a video stream.");
                return false;
            }

            Instance.SetDuration(stream.Duration);
            Instance.AllocateVideoTexture(stream.Width, stream.Height);
        }
        catch (Exception ex)
        {
            VideoInstance.Logger.Error($"FFmpegVideoBackend.Initialize failed: {ex}");
            ReleaseMedia();
            return false;
        }
        return true;
    }

    public override void ReleaseMedia()
    {
        if (media == null)
            return;
        media.RemoveDisposeBy(Instance);
        media.Dispose();
        media = null;
        stream = null;
    }

    public override void Play()
    {
        if (Instance.PlayRange.Start > Instance.CurrentTime)
        {
            Instance.SetCurrentTime(Instance.PlayRange.Start);
            Instance.Seek(Instance.PlayRange.Start);
        }
    }

    public override void Stop()
    {
        Instance.Seek(Instance.PlayRange.Start);
    }

    public override void Seek(TimeSpan time)
    {
        media.SeekToTime(stream, stream.TimeToTimestamp(time));
        adjustedTicksSinceLastFrame = stream.FrameDuration.Ticks; // first frame loads immediately
    }

    public override void Update(TimeSpan elapsed)
    {
        if (stream == null || Instance.PlayState == PlayState.Stopped)
            return;

        var speedFactor = Instance.SpeedFactor;
        if (Instance.PlayState == PlayState.Paused)
            speedFactor = 0;

        var frameDurationTicks = stream.FrameDuration.Ticks;
        adjustedTicksSinceLastFrame += (long)(elapsed.Ticks * speedFactor);
        if (adjustedTicksSinceLastFrame < frameDurationTicks)
            return;

        var frameCount = (int)(adjustedTicksSinceLastFrame / frameDurationTicks);
        if (frameCount == 0)
            return;

        if (frameCount > 4)
        {
            // FIXME: heuristic may need tuning per codec / resolution / file size.
            Instance.Seek(Instance.CurrentTime + TimeSpan.FromTicks(frameDurationTicks * frameCount));
            frameCount = 1;
        }

        var extractedFrameCount = media.ExtractFrames(stream, frameCount);
        if (extractedFrameCount > 0)
            adjustedTicksSinceLastFrame = adjustedTicksSinceLastFrame % stream.FrameDuration.Ticks;

        var streamInfo = media.GetStreamInfo(stream);
        if (streamInfo?.Image == null)
            return;

        var endOfMedia = streamInfo.ReachedEnd;
        if (!endOfMedia)
        {
            if (extractedFrameCount > 0)
                Instance.SetCurrentTime(stream.TimestampToTime(streamInfo.Image.Timestamp));

            if (Instance.PlayRange.IsValid() && Instance.CurrentTime > Instance.PlayRange.End)
                endOfMedia = true;
            else if (Instance.IsLooping && Instance.LoopRange.IsValid() && Instance.CurrentTime > Instance.LoopRange.End)
                endOfMedia = true;
        }

        if (endOfMedia)
        {
            if (Instance.IsLooping)
            {
                Instance.SetCurrentTime(Instance.LoopRange.Start);
                Instance.Seek(Instance.LoopRange.Start);
                return;
            }
            Instance.Stop();
            return;
        }

        if (extractedFrameCount == 0)
            return;

        var target = Instance.VideoComponent.Target;
        if (target != null)
        {
            // Upload decoded RGBA directly into the user's Target. We bypass VideoTexture's
            // Swap-based ping-pong: it stashes the last frame in an internal texture and
            // reverts the user Target on Stop(), making the decoded frame disappear after EOF.
            var graphicsContext = Instance.Services.GetSafeServiceAs<GraphicsContext>();
            unsafe
            {
                var data = new ReadOnlySpan<byte>((void*)streamInfo.Image.Buffer, streamInfo.Image.BufferSize);
                target.SetData(graphicsContext.CommandList, data, arrayIndex: 0, mipLevel: 0);
            }
        }
    }
}
#endif
