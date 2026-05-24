// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Video;

/// <summary>
/// Per-<see cref="VideoInstance"/> playback backend. Concrete subclasses wrap a specific decoding
/// implementation (FFmpeg, Windows Media Foundation, Android MediaCodec, ...). Instances are
/// produced by <see cref="VideoBackendFactory.CreateBackend"/> at media-initialization time.
/// </summary>
public abstract class VideoBackend : IDisposable
{
    protected VideoBackend(VideoInstance instance)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
    }

    protected VideoInstance Instance { get; }

    /// <summary>True when this backend currently decodes via hardware (e.g. D3D11VA, NVDEC).
    /// May only become accurate after <see cref="Initialize"/> and possibly <see cref="Play"/>,
    /// depending on when the backend negotiates the codec.</summary>
    public virtual bool UsesHardwareDecode => false;

    /// <summary>Open <paramref name="url"/> for playback. Return false on failure.</summary>
    public abstract bool Initialize(string url, long startPosition, long length);

    /// <summary>Release the currently-open media. Counterpart to <see cref="Initialize"/>.</summary>
    public virtual void ReleaseMedia() { }

    public abstract void Play();
    public virtual void Pause() { }
    public abstract void Stop();
    public virtual void Seek(TimeSpan time) { }
    public virtual void SetPlaybackSpeed(float speed) { }
    public virtual void SetAudioVolume(float volume) { }
    public virtual void UpdatePlayRange() { }
    public virtual void UpdateLoopRange() { }

    /// <summary>Per-frame tick. May update <see cref="VideoInstance.CurrentTime"/> via
    /// <see cref="VideoInstance.SetCurrentTime"/>, and may call back into <see cref="VideoInstance.Stop"/>
    /// or <see cref="VideoInstance.Seek"/> on end-of-media / loop conditions.</summary>
    public abstract void Update(TimeSpan elapsed);

    public virtual void Dispose() => ReleaseMedia();
}
